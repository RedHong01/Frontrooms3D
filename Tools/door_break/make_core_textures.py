"""Cut-face and damage textures for the door break (prototype DB2, production P2 2026-10-30; research/door_break/50_rethink.md
3.8, 4.3 item 4; 52_production.md). numpy + PIL, deterministic (seeded). P2 adds: the walnut face veneer in the strip
(Red's door wood, DoorVeneer_A), Door_CoreSteel, and the _FR_DAMAGE onset mask Door_Damage_Veneer_{O,N}.

  python Tools/door_break/make_core_textures.py [--out <project>/Assets/Resources/Surfaces/Textures]

Door_CoreWood_{A,N,S}.png  1024 x 512: the CROSS-SECTION STRIP of a 44 mm solid-core flush leaf, mapped across U by
    depth (mesh UV U = X + 0.022 m, tile 0.044 m) and along V by the grain (tile 0.5 m). Mirrored about the centre:
    0.6 mm face veneer (the DoorVeneer_A tone), a dark glue line, 1.6 mm crossband (pale, cross-grain fibre ends),
    a glue line, a fine dense particleboard face layer (~2.5 mm), then the coarse low-density core with voids
    [T-PB in SOURCES.md]. 1024 x 512 rather than the design's 2048 x 256: 23 px/mm across, 1 px/mm along (the
    design's 256 rows gave 2 mm per texel along the grain, half the close-up's screen density).
Door_CoreStile_{A,N,S}.png 1024 x 1024: a long-fibre SPLIT face of the hardwood stile and lock block (tile 0.05 x 0.25 m:
    20 px/mm across, 4 px/mm along): torn fibres along V, lighter and matte (the inside of the wood, never finished),
    from Prop_WoodOak_A's tone, turned so its grain runs along V.
Door_CoreSteel_{A,N,S}.png 1024 x 512 (P3 uses it; P2 makes the surface): the cross-section of a hollow-metal leaf's edge
    seam, mapped like Door_CoreWood (U = depth across 44 mm, tile 0.044 m; V along the seam, tile 0.25 m): an 18 ga
    skin (1.2 mm) as bare steel at the cut with a primer line, a glue line, then kraft paper honeycomb cut through
    (brown paper walls, dark open cells) [RF26][S24][T-HM].
Door_Damage_Veneer_O.png  512 x 512 RGBA, LINEAR: the _FR_DAMAGE onset map (50_rethink 3.8) in leaf OBJECT space,
    canonical burst toward +X. Left half = the burst face (+X), right half = the push face (-X); each half covers
    Z 0.595..0.995 (U) x Y 0.80..1.40 (V). R = the stage at which a texel turns (0.25 M, 0.5 D1, 0.75 D2, 1.0 TORN;
    1.0 = never before TORN, 0 = unused), G = height (0.5 flat; 0 = 6 mm below), B = fibre (crushed / lifted veneer:
    lighter, matte), A = fines (dust wipe). Motif-independent: the split itself is geometry in the cassettes.
Door_Damage_Veneer_N.png  512 x 512, the tangent-space normal of G (U along +Z, V along +Y on both faces).
P3 (2026-10-30, own seeds; the files above are unchanged): Door_CoreGypsum_{A,N,S} (the frame wall's gypsum core, for the
    steel break's crumbs) and Door_Damage_Enamel_{O,N} (the steel leaf's onset map: Y 0.50..1.22, crazing and primer chips).
_S = mask: R smoothness, G cavity (FrontRoomsSurface). _N = tangent-space normal from a height field.
"""
import argparse
import os

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
TEX = os.path.join(ROOT, "Assets", "Resources", "Surfaces", "Textures")


# DoorVeneer_A (plain-sliced walnut, landed 10-30) measured mean sRGB 132, 81, 61 (54_dust_grit 2.2); the cut veneer edge
# shows the same wood a shade lighter (no lacquer on the cut).
WALNUT_SRGB = [140, 88, 66]


def srgb_to_lin(c):
    c = np.asarray(c, dtype=np.float64) / 255.0
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def lin_to_srgb8(c):
    c = np.clip(c, 0, 1)
    s = np.where(c <= 0.0031308, c * 12.92, 1.055 * np.power(c, 1 / 2.4) - 0.055)
    return np.clip(np.round(s * 255), 0, 255).astype(np.uint8)


def smooth_noise(rng, shape, cells):
    """Value noise: a coarse random grid (cells = (ny, nx)) upsampled bicubic-like (two bilinear passes)."""
    g = rng.random((cells[0] + 1, cells[1] + 1))
    im = Image.fromarray((g * 65535).astype(np.uint16).astype(np.int32), mode="I")
    im = im.resize((shape[1], shape[0]), Image.BICUBIC)
    a = np.asarray(im, dtype=np.float64) / 65535.0
    return np.clip(a, 0, 1)


def normal_from_height(h, sx, sy, strength):
    """h in metres per texel grid; sx, sy = metres per texel along U and V. OpenGL-style +Y (Unity)."""
    dx = (np.roll(h, -1, axis=1) - np.roll(h, 1, axis=1)) / (2 * sx)
    dy = (np.roll(h, -1, axis=0) - np.roll(h, 1, axis=0)) / (2 * sy)
    n = np.dstack([-dx * strength, dy * strength, np.ones_like(h)])
    n /= np.linalg.norm(n, axis=2, keepdims=True)
    return np.clip(np.round((n * 0.5 + 0.5) * 255), 0, 255).astype(np.uint8)


def chips(rng, H, W, mm_u, mm_v, size_mm, density):
    """Particleboard chips: random flat flakes (rectangles at physical size) with their own tones; returns
    (tone, height, void) arrays."""
    tone = np.zeros((H, W))
    height = np.zeros((H, W))
    n = int(density * (W * mm_u) * (H * mm_v) / (size_mm * size_mm))
    for _ in range(n):
        cu, cv = rng.random() * W, rng.random() * H
        a = size_mm * (0.5 + rng.random())
        b = size_mm * (0.25 + 0.6 * rng.random())
        if rng.random() < 0.5:
            a, b = b, a
        hu, hv = max(1, int(a / mm_u / 2)), max(1, int(b / mm_v / 2))
        u0, u1 = int(cu) - hu, int(cu) + hu
        v0, v1 = int(cv) - hv, int(cv) + hv
        t = rng.normal(0, 1)
        hh = rng.random()
        us = np.arange(u0, u1 + 1) % W
        vs = np.arange(v0, v1 + 1) % H
        tone[np.ix_(vs, us)] = t
        height[np.ix_(vs, us)] = hh
    return tone, height


def core_wood(rng):
    W, H = 1024, 512
    mm_u = 44.0 / W            # mm per texel across
    mm_v = 500.0 / H           # mm per texel along the grain
    x = (np.arange(W) + 0.5) * mm_u
    d = np.minimum(x, 44.0 - x)                       # depth from the nearest face (mirrored)
    veneer = srgb_to_lin(WALNUT_SRGB)             # P2: the walnut face veneer (DoorVeneer_A mean, Red 10-30)
    glue = srgb_to_lin([70, 48, 28])
    cross = srgb_to_lin([205, 168, 120])
    pb_fine = srgb_to_lin([190, 160, 118])
    pb_core = srgb_to_lin([168, 138, 100])
    void = srgb_to_lin([60, 44, 30])
    layer = np.zeros(W, dtype=int)
    layer[d < 0.6] = 0
    layer[(d >= 0.6) & (d < 0.75)] = 1
    layer[(d >= 0.75) & (d < 2.35)] = 2
    layer[(d >= 2.35) & (d < 2.5)] = 1
    layer[(d >= 2.5) & (d < 5.0)] = 3
    layer[d >= 5.0] = 4
    L = np.broadcast_to(layer, (H, W))
    alb = np.zeros((H, W, 3))
    hgt = np.zeros((H, W))
    rough = np.zeros((H, W))
    cav = np.ones((H, W))
    fine_t, fine_h = chips(rng, H, W, mm_u, mm_v, 0.8, 1.6)
    core_t, core_h = chips(rng, H, W, mm_u, mm_v, 2.6, 1.3)
    voids = smooth_noise(rng, (H, W), (H // 2, int(W * mm_u / 1.2)))
    fib = smooth_noise(rng, (H, W), (H, int(W * mm_u / 0.25)))     # crossband fibre ends: fine dots across
    vn = smooth_noise(rng, (H, W), (H // 8, 8))                     # veneer grain along V
    for k, base in ((0, veneer), (1, glue), (2, cross), (3, pb_fine), (4, pb_core)):
        m = L == k
        if k == 0:
            t = 1.0 + 0.10 * (vn - 0.5)
            alb[m] = base * t[m][:, None]
            rough[m] = 0.32
            hgt[m] = 0.00005 * vn[m]
        elif k == 1:
            alb[m] = base
            rough[m] = 0.2
            hgt[m] = -0.00008
            cav[m] = 0.75
        elif k == 2:
            t = 1.0 + 0.18 * (fib - 0.5)
            alb[m] = base * t[m][:, None]
            rough[m] = 0.18
            hgt[m] = 0.00006 * fib[m]
        else:
            ct, ch = (fine_t, fine_h) if k == 3 else (core_t, core_h)
            sat = 0.07 if k == 3 else 0.11
            t = 1.0 + sat * ct
            alb[m] = base * np.clip(t[m], 0.6, 1.4)[:, None]
            hgt[m] = (0.00012 if k == 3 else 0.00035) * ch[m]
            rough[m] = 0.12
            if k == 4:
                vm = m & (voids > 0.80)
                alb[vm] = void * (0.8 + 0.4 * voids[vm])[:, None]
                hgt[vm] = -0.0008
                cav[vm] = 0.35
    A = lin_to_srgb8(alb)
    N = normal_from_height(hgt, mm_u / 1000.0, mm_v / 1000.0, 0.6)
    S = np.dstack([np.round(rough * 255), np.round(cav * 255), np.zeros((H, W)), np.full((H, W), 255)]).astype(np.uint8)
    return A, N, S


def core_stile(rng):
    W, H = 1024, 1024
    mm_u, mm_v = 50.0 / W, 250.0 / H
    base = srgb_to_lin([196, 150, 98])               # raw oak inside: lighter than the finished veneer
    streak = smooth_noise(rng, (H, W), (6, int(W * mm_u / 0.35)))          # long fibres: fine across, long along
    streak2 = smooth_noise(rng, (H, W), (20, int(W * mm_u / 1.2)))
    torn = smooth_noise(rng, (H, W), (int(H * mm_v / 6), int(W * mm_u / 3)))
    t = 1.0 + 0.16 * (streak - 0.5) + 0.10 * (streak2 - 0.5) + 0.12 * (torn - 0.5)
    alb = base[None, None, :] * t[:, :, None]
    # pale torn fibre tufts
    tuft = torn > 0.78
    alb[tuft] = alb[tuft] * 1.12
    hgt = 0.0004 * streak + 0.0003 * streak2 + 0.0012 * (torn - 0.5)
    rough = np.full((H, W), 0.14)
    cav = np.clip(0.55 + 0.6 * streak2 * (0.6 + 0.4 * torn), 0, 1)
    A = lin_to_srgb8(alb)
    N = normal_from_height(hgt, mm_u / 1000.0, mm_v / 1000.0, 0.5)
    S = np.dstack([np.round(rough * 255), np.round(cav * 255), np.zeros((H, W)), np.full((H, W), 255)]).astype(np.uint8)
    return A, N, S


def core_steel(rng):
    W, H = 1024, 512
    mm_u, mm_v = 44.0 / W, 250.0 / H
    x = (np.arange(W) + 0.5) * mm_u
    d = np.minimum(x, 44.0 - x)
    steel = srgb_to_lin([150, 150, 146])
    primer = srgb_to_lin([142, 118, 96])               # red-oxide-grey primer line under the enamel, seen at the cut
    glue = srgb_to_lin([88, 70, 44])
    kraft = srgb_to_lin([156, 116, 72])
    cell = srgb_to_lin([46, 34, 22])
    D = np.broadcast_to(d, (H, W))
    alb = np.zeros((H, W, 3))
    hgt = np.zeros((H, W))
    rough = np.zeros((H, W))
    cav = np.ones((H, W))
    grain = smooth_noise(rng, (H, W), (H // 4, 16))
    # the honeycomb cut through: paper walls every ~12.7 mm across (1/2" cells), wavy along V, 0.25 mm thick
    vv = (np.arange(H) + 0.5)[:, None] * mm_v
    wav = 1.2 * np.sin(vv / 9.0 * 2 * np.pi + rng.random() * 6.28)
    pos = (D - 1.45 + wav) % 12.7
    wall = (np.minimum(pos, 12.7 - pos) < 0.18) | ((vv % 22.0) < 0.22)
    sk = D < 1.2
    alb[sk] = steel * (0.92 + 0.16 * grain[sk])[:, None]
    rough[sk] = 0.55
    hgt[sk] = 0.00004
    pr = (D >= 1.2) & (D < 1.32)
    alb[pr] = primer
    rough[pr] = 0.3
    gl = (D >= 1.32) & (D < 1.45)
    alb[gl] = glue
    rough[gl] = 0.2
    hc = D >= 1.45
    open_cell = hc & ~wall
    alb[open_cell] = cell * (0.7 + 0.6 * grain[open_cell])[:, None]
    hgt[open_cell] = -0.004
    cav[open_cell] = 0.25
    rough[open_cell] = 0.1
    pw = hc & wall
    alb[pw] = kraft * (0.9 + 0.2 * grain[pw])[:, None]
    hgt[pw] = 0.0
    rough[pw] = 0.12
    A = lin_to_srgb8(alb)
    N = normal_from_height(hgt, mm_u / 1000.0, mm_v / 1000.0, 0.35)
    S = np.dstack([np.round(rough * 255), np.round(cav * 255), np.zeros((H, W)), np.full((H, W), 255)]).astype(np.uint8)
    return A, N, S


# ---- the _FR_DAMAGE onset map (P2; 50_rethink 3.8) -----------------------------------------------------------------------
ONSET_Z0, ONSET_Y0, ONSET_DZ, ONSET_DY = 0.595, 0.80, 0.40, 0.60
BORE = (1.000, 0.920)          # (Y, Z), door root, canonical +X burst
ROSE_R = 0.032                 # the lever rose on each face covers the bore by this radius
CONTACT = (1.30, 0.745)        # the Relay's forearm on the push face (cassette sidecar contact_push)


def onset_maps(rng):
    """(O RGBA uint8, N RGB uint8), 512 x 512, rows bottom-up in the arrays (flipped to PNG rows at save)."""
    HW, H = 256, 512
    zs = ONSET_Z0 + (np.arange(HW) + 0.5) / HW * ONSET_DZ
    ys = ONSET_Y0 + (np.arange(H) + 0.5) / H * ONSET_DY
    Z, Y = np.meshgrid(zs, ys)
    out = []
    for face in (0, 1):
        onset = np.ones((H, HW))            # 1.0 = turns at TORN at the earliest
        mask = np.zeros((H, HW))            # where the onset is meaningful
        g = np.full((H, HW), 0.5)
        b = np.zeros((H, HW))
        a = np.zeros((H, HW))

        def put(m, st, gv=None, bv=None, av=None):
            sel = m & ((mask == 0) | (onset > st))
            onset[sel] = st
            mask[m] = 1
            if gv is not None:
                g[m] = np.minimum(g[m], gv[m] if np.ndim(gv) else gv)
            if bv is not None:
                b[m] = np.maximum(b[m], bv[m] if np.ndim(bv) else bv)
            if av is not None:
                a[m] = np.maximum(a[m], av[m] if np.ndim(av) else av)
        wander = smooth_noise(rng, (H, HW), (24, 6)) - 0.5
        fine = smooth_noise(rng, (H, HW), (H // 3, HW // 3))
        if face == 0:
            # M: hairlines along the grain out of the rose's edge (up and down), 1-1.5 mm grooves with lifted fibre
            for zc, ya, yb, st in ((0.914, 1.0 + ROSE_R, 1.165, 0.25), (0.929, 1.0 + ROSE_R, 1.105, 0.25), (0.921, 0.855, 1.0 - ROSE_R, 0.25),
                                   (0.903, 1.04, 1.24, 0.5), (0.936, 0.78, 0.97, 0.5), (0.884, 1.10, 1.30, 0.75), (0.952, 0.82, 0.93, 0.75)):
                zl = zc + 0.0025 * wander[:, :] * 2
                m = (np.abs(Z - zl) < 0.0009) & (Y > ya) & (Y < yb)
                taper = np.clip(np.minimum(Y - ya, yb - Y) / 0.03, 0.3, 1.0)
                put(m, st, gv=0.5 - 0.18 * taper, bv=0.55 * taper)
            # D1: the latch edge's arris crushed where the strip stood proud (lifted, whitened fibre)
            m = (Z > 0.976) & (Y > 0.80) & (Y < 1.24)
            put(m, 0.5, gv=0.44 + 0.04 * fine, bv=0.35 + 0.35 * fine)
            # D2: fines wiped down the face below the slot (gravity streaks), and veneer checks by the petal
            streak = smooth_noise(rng, (H, HW), (6, 90))
            m = (Z > 0.875) & (Z < 0.955) & (Y < 0.99) & (streak > 0.45)
            put(m, 0.75, av=np.clip((streak - 0.45) * 2.2, 0, 1) * np.clip((0.99 - Y) / 0.05, 0, 1) * np.clip((Y - 0.80) / 0.08, 0, 1))
            m = (Z > 0.80) & (Z < 0.86) & (Y > 1.04) & (Y < 1.32) & (np.abs(((Z - 0.80) * 140 + wander * 3) % 1.0 - 0.5) < 0.05)
            put(m, 0.75, gv=0.46, bv=0.45)
            # TORN: the notch's hinge-side lip: whitened torn veneer and fines
            m = (Z > 0.862) & (Z < 0.905) & (Y > 0.72) & (Y < 1.38)
            w = np.clip((Z - 0.862) / 0.043, 0, 1)
            put(m, 1.0, bv=0.55 * w * (0.6 + 0.4 * fine), av=0.5 * w)
        else:
            # the Relay's contact on the push face: a growing dish (M 2 mm, D1 4 mm, D2 6 mm deep, 50_rethink 3.3)
            r = np.sqrt(((Z - CONTACT[1]) / 1.0) ** 2 + ((Y - CONTACT[0]) / 1.15) ** 2)
            ragged = 0.006 * (fine - 0.5)
            for rr, st in ((0.045, 0.25), (0.058, 0.5), (0.070, 0.75), (0.080, 1.0)):
                put(r + ragged < rr, st)
            d = np.clip(1.0 - (r / 0.072) ** 2, 0, 1) ** 2
            g = np.where(mask > 0, np.minimum(g, 0.5 - 0.5 * d), g)
            # crushed-veneer wrinkles round the dish at D1 (radial)
            ang = np.arctan2(Y - CONTACT[0], Z - CONTACT[1])
            wr = (0.5 + 0.5 * np.sin(ang * 23.0 + 6.0 * fine)) * np.clip(1 - np.abs(r - 0.05) / 0.018, 0, 1)
            m = wr > 0.25
            put(m, 0.5, bv=0.6 * wr)
            # the push-face rose pushed in: a crushed ring at the rose's edge (D1)
            rb = np.sqrt((Z - BORE[1]) ** 2 + (Y - BORE[0]) ** 2)
            m = (rb > ROSE_R) & (rb < ROSE_R + 0.007)
            put(m, 0.5, gv=0.40, bv=0.4)
            # D2: the strip's back edge stood proud: crushed latch arris on the push face
            m = (Z > 0.978) & (Y > 0.70) & (Y < 1.32)
            put(m, 0.75, gv=0.45, bv=0.35 + 0.3 * fine)
        onset = np.where(mask > 0, onset, 0.0)
        out.append((onset, g, b, a))
    O = np.concatenate([np.dstack(o) for o in out], axis=1)          # H x 512 x 4
    hgt = (O[:, :, 1] - 0.5) * 2.0 * 0.006
    N = normal_from_height(hgt, ONSET_DZ / 256.0, ONSET_DY / 512.0, 1.0)
    O8 = np.clip(np.round(O * 255), 0, 255).astype(np.uint8)
    return O8[::-1], N[::-1]


# ---- P3 (steel), 2026-10-30: the gypsum core of the frame's wall (crumbs), and the enamel onset map ----------------------
def core_gypsum(rng):
    """Door_CoreGypsum_{A,N,S} 512 x 512 (tile 0.05 m): the broken core of 1990 gypsum board behind a frame's anchors, off-white
    (sRGB 225, 222, 212 to 205, 203, 196; 54_dust_grit 2.2) with grey aggregate specks and small voids, matte."""
    W = H = 512
    hi, lo = srgb_to_lin([225, 222, 212]), srgb_to_lin([205, 203, 196])
    n1 = smooth_noise(rng, (H, W), (32, 32))
    n2 = smooth_noise(rng, (H, W), (128, 128))
    t = np.clip(0.55 * n1 + 0.45 * n2, 0, 1)
    alb = lo[None, None, :] + (hi - lo)[None, None, :] * t[:, :, None]
    speck = rng.random((H, W)) < 0.012
    alb[speck] *= 0.78
    void = smooth_noise(rng, (H, W), (64, 64)) > 0.83
    alb[void] *= 0.62
    hgt = (n2 - 0.5) * 0.0004 - void * 0.0008
    A = lin_to_srgb8(alb)
    N = normal_from_height(hgt, 0.05 / W, 0.05 / H, 1.0)
    rough = np.full((H, W), 0.08)
    cav = np.where(void, 0.4, 1.0)
    S = np.dstack([np.round(rough * 255), np.round(cav * 255), np.zeros((H, W)), np.full((H, W), 255)]).astype(np.uint8)
    return A, N, S


EN_Z0, EN_Y0, EN_DZ, EN_DY = 0.595, 0.50, 0.40, 0.72     # the steel window (Y 0.50..1.22) plus 5 mm
EN_CONTACT = (1.08, 0.745)                               # the Relay's dish (motif centres jitter +-0.02)


def onset_enamel(rng):
    """Door_Damage_Enamel_{O,N} 512 x 512 (O RGBA linear): the steel leaf's onset map, laid out like Door_Damage_Veneer (left
    half the burst face, right half the push face; U = Z 0.595..0.995, V = Y 0.50..1.22). The dents are geometry (the
    cassettes); the map carries the PAINT: R onset (0.25 M .. 1.0 TORN), G a hairline cavity (0.5 flat), B crazed enamel
    (lighter, matte: the shader's B), A chips down to the primer (the shader's fines wipe; the stage materials set the fines
    colour to the red-oxide-grey primer)."""
    HW, H = 256, 512
    zs = EN_Z0 + (np.arange(HW) + 0.5) / HW * EN_DZ
    ys = EN_Y0 + (np.arange(H) + 0.5) / H * EN_DY
    Z, Y = np.meshgrid(zs, ys)
    out = []
    for face in (0, 1):
        onset = np.ones((H, HW))
        mask = np.zeros((H, HW))
        g = np.full((H, HW), 0.5)
        b = np.zeros((H, HW))
        a = np.zeros((H, HW))

        def put(m, st, gv=None, bv=None, av=None):
            sel = m & ((mask == 0) | (onset > st))
            onset[sel] = st
            mask[m] = 1
            if gv is not None:
                g[m] = np.minimum(g[m], gv[m] if np.ndim(gv) else gv)
            if bv is not None:
                b[m] = np.maximum(b[m], bv[m] if np.ndim(bv) else bv)
            if av is not None:
                a[m] = np.maximum(a[m], av[m] if np.ndim(av) else av)
        fine = smooth_noise(rng, (H, HW), (H // 4, HW // 4))
        cells = smooth_noise(rng, (H, HW), (H // 6, HW // 6))
        # crazing: a cell network (the edges of a jittered value-noise field), a fraction of a mm wide
        craze = np.abs(cells - 0.5) < 0.035
        flecks = smooth_noise(rng, (H, HW), (H // 3, HW // 3)) > 0.78
        if face == 0:
            # D1: the bow's tightest bend round the lock edge crazes (tension side)
            r = np.sqrt(((Z - 0.975) / 0.07) ** 2 + ((Y - 0.98) / 0.20) ** 2)
            m = (r < 1.0) & craze
            put(m, 0.5, gv=0.44, bv=0.55 * (1 - r))
            # D2: chips to the primer along the crushed lock edge, and the peeling seam's lip (below the armor front)
            m = (Z > 0.982) & (Y > 0.80) & (Y < 1.16) & flecks
            put(m, 0.75, bv=0.3, av=0.85)
            m = (Z > 0.962) & (Y > 0.57) & (Y < 0.88) & (craze | flecks)
            w = np.clip((Z - 0.962) / 0.033, 0, 1)
            put(m, 0.75, gv=0.45, bv=0.5 * w, av=np.where(flecks, 0.9 * w, 0.0))
            # TORN: the second peel above the armor front
            m = (Z > 0.968) & (Y > 1.07) & (Y < 1.21) & (craze | flecks)
            put(m, 1.0, bv=0.45, av=np.where(flecks, 0.8, 0.0))
        else:
            # the push face's dish: a star of radial cracks at M, crazing rings at D1, chips to the primer at D1/D2
            dz, dy = Z - EN_CONTACT[1], Y - EN_CONTACT[0]
            r = np.sqrt(dz ** 2 + (dy / 1.15) ** 2)
            ang = np.arctan2(dy, dz)
            star = (np.abs(np.sin(ang * 7.0 + 3.0 * fine)) < 0.06 + 0.04 * (1 - np.clip(r / 0.07, 0, 1))) & (r < 0.07)
            put(star, 0.25, gv=0.44, bv=0.6)
            put((r < 0.02) & craze, 0.25, gv=0.45, bv=0.5)
            ring = (np.abs(r - 0.075 - 0.01 * (fine - 0.5)) < 0.018) & craze
            put(ring, 0.5, gv=0.44, bv=0.55)
            put((r < 0.085) & flecks & (fine > 0.55), 0.5, bv=0.3, av=0.9)
            put((r < 0.11) & flecks, 0.75, bv=0.35, av=0.95)
            # the crushed lock edge seen from the push side (D2)
            put((Z > 0.982) & (Y > 0.82) & (Y < 1.15) & (craze | flecks), 0.75, gv=0.45, bv=0.4, av=np.where(flecks, 0.7, 0.0))
        onset = np.where(mask > 0, onset, 0.0)
        out.append((onset, g, b, a))
    O = np.concatenate([np.dstack(o) for o in out], axis=1)
    hgt = (O[:, :, 1] - 0.5) * 2.0 * 0.0015
    N = normal_from_height(hgt, EN_DZ / 256.0, EN_DY / 512.0, 1.0)
    O8 = np.clip(np.round(O * 255), 0, 255).astype(np.uint8)
    return O8[::-1], N[::-1]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default=TEX)
    a = ap.parse_args()
    os.makedirs(a.out, exist_ok=True)
    rng = np.random.default_rng(20261030)
    for name, fn in (("Door_CoreWood", core_wood), ("Door_CoreStile", core_stile), ("Door_CoreSteel", core_steel)):
        A, N, S = fn(rng)
        Image.fromarray(A, "RGB").save(os.path.join(a.out, name + "_A.png"))
        Image.fromarray(N, "RGB").save(os.path.join(a.out, name + "_N.png"))
        Image.fromarray(S, "RGBA").save(os.path.join(a.out, name + "_S.png"))
        print("[tex] %s %dx%d  albedo mean %s" % (name, A.shape[1], A.shape[0], A.reshape(-1, 3).mean(0).round(1)))
    O, N = onset_maps(np.random.default_rng(20261031))
    Image.fromarray(O, "RGBA").save(os.path.join(a.out, "Door_Damage_Veneer_O.png"))
    Image.fromarray(N, "RGB").save(os.path.join(a.out, "Door_Damage_Veneer_N.png"))
    used = O[:, :, 0] > 0
    print("[tex] Door_Damage_Veneer_O 512x512: %d texels used; by stage M %d D1 %d D2 %d TORN %d" % (
        int(used.sum()), int((O[:, :, 0] == 64).sum()), int((O[:, :, 0] == 128).sum()), int((O[:, :, 0] == 191).sum()), int((O[:, :, 0] == 255).sum())))
    # P3 (2026-10-30): own seeds, so the textures above stay byte-identical
    A, N, S = core_gypsum(np.random.default_rng(20261033))
    Image.fromarray(A, "RGB").save(os.path.join(a.out, "Door_CoreGypsum_A.png"))
    Image.fromarray(N, "RGB").save(os.path.join(a.out, "Door_CoreGypsum_N.png"))
    Image.fromarray(S, "RGBA").save(os.path.join(a.out, "Door_CoreGypsum_S.png"))
    print("[tex] Door_CoreGypsum 512x512  albedo mean %s" % A.reshape(-1, 3).mean(0).round(1))
    O, N = onset_enamel(np.random.default_rng(20261032))
    Image.fromarray(O, "RGBA").save(os.path.join(a.out, "Door_Damage_Enamel_O.png"))
    Image.fromarray(N, "RGB").save(os.path.join(a.out, "Door_Damage_Enamel_N.png"))
    used = O[:, :, 0] > 0
    print("[tex] Door_Damage_Enamel_O 512x512: %d texels used; by stage M %d D1 %d D2 %d TORN %d" % (
        int(used.sum()), int((O[:, :, 0] == 64).sum()), int((O[:, :, 0] == 128).sum()), int((O[:, :, 0] == 191).sum()), int((O[:, :, 0] == 255).sum())))


if __name__ == "__main__":
    main()
