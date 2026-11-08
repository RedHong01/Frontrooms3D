"""FRONTROOMSSS structures surfaces of group G1-b (structures_assets step 2, line visual (game visual), 2026-11-06; lands as
Tools/lookdev/structures/gen_structures_g1b.py with P6 / P9).

Run: venv/bin/python gen_structures_g1b.py <out dir>
Writes (house style of Tools/lookdev/gen_common.py and zone_lamps' painters: numpy + Pillow, OpenGL +Y normals,
_A sRGB albedo, _N linear normal, _S linear mask R smoothness G cavity B metal A 1, _E emission sRGB-encoded linear):
  Prop_LaylightDiffuser_{A,N,S,E}.png  1024^2: a 4 x 4 atlas of 600 mm prismatic diffuser panels (2.4 m tile, _TileSize
      2.4 on mesh-UV metres; the module kit points each panel at one cell). Two fluorescent rows faint through every panel
      (lamps along the module's X), edges dimmer under the T-bars, per-panel age (yellowing, dust, insects), one replaced
      panel whiter, two cells with one row dead (13 north, 14 south), one dusty-edged (15). Emission relative (peak 1.0):
      the lens drive (FrontRoomsStructuresLamps.DriveLens) gives level and colour. 20_research.md F12, C.4.6.
  Prop_LaylightDiffuser_D.png  512^2 detail map (RG slopes 0.5 + slope x 4, B gloss, A tone): the cone prisms at 1/4 in
      pitch over a 1 in span (FrontRooms/Surface _FR_DETAIL fine pass, kind 99 = no kind term; faded where sub-pixel).
  Prop_LaylightDiffuser_Phone_{A,N,S,E}.png  512^2 halves.
  Prop_FRPGrating_{A,N,S}.png  512^2 tiling 0.30 m: moulded fibreglass grating, light grey resin, quartz grit on top.
  Prop_FlexConduit_{A,N,S}.png  256 x 128 tiling 36 x 53.4 mm: 3/8 in flexible metal conduit, four convolutions of the
      interlocked galvanised strip a tile (u along the whip, v round it).
No text, label, brand or logo. ASCII only.
"""
import math
import os
import sys

import numpy as np
from PIL import Image

OUT = sys.argv[1] if len(sys.argv) > 1 else "."
os.makedirs(OUT, exist_ok=True)


def spectral(h, w, beta, seed):
    r = np.random.default_rng(seed)
    F = np.fft.fft2(r.standard_normal((h, w)))
    fy = np.fft.fftfreq(h)[:, None] * h
    fx = np.fft.fftfreq(w)[None, :] * w
    f = np.sqrt(fx * fx + fy * fy)
    f[0, 0] = 1
    amp = 1.0 / np.power(np.maximum(f, 1.0), beta)
    amp[0, 0] = 0
    n = np.real(np.fft.ifft2(F * amp))
    n -= n.min()
    n /= max(1e-8, n.max())
    return n.astype(np.float32)


def lin_to_srgb(c):
    c = np.clip(c, 0, 1)
    return np.where(c <= .0031308, c * 12.92, 1.055 * c ** (1 / 2.4) - .055)


def srgb_to_lin(c):
    c = np.clip(c, 0, 1)
    return np.where(c <= .04045, c / 12.92, ((c + .055) / 1.055) ** 2.4)


def hexc(s):
    return np.array([int(s[i:i + 2], 16) / 255 for i in (1, 3, 5)], np.float32)


def smooth(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def save(name, arr, mode):
    a = np.clip(arr, 0, 1)
    Image.fromarray((a * 255 + .5).astype(np.uint8), mode).save(os.path.join(OUT, name + ".png"), optimize=True)


def normal_from_grad(gx, gy):
    n = np.stack([-gx, -gy, np.ones_like(gx)], -1)
    return n / np.linalg.norm(n, axis=-1, keepdims=True)


def down2(a):
    h, w = a.shape[:2]
    return a.reshape(h // 2, 2, w // 2, 2, *a.shape[2:]).mean(axis=(1, 3))


# ============================================================================ the laylight diffuser
CELL = 256
PANEL_MM = 600.0
CELLS = {
    # cell: (yellowing, dust, insects, emission gain, dead band: None / 'n' / 's', dusty edges)
    0: (0.020, 0.03, 0, 1.00, None, 0.0), 1: (0.035, 0.05, 1, 0.98, None, 0.0), 2: (0.015, 0.02, 0, 1.01, None, 0.0),
    3: (0.045, 0.06, 2, 0.97, None, 0.1), 4: (0.028, 0.04, 0, 0.99, None, 0.0), 5: (0.050, 0.07, 1, 0.96, None, 0.2),
    6: (0.022, 0.03, 0, 1.00, None, 0.0), 7: (0.038, 0.05, 3, 0.98, None, 0.0), 8: (0.030, 0.04, 0, 0.99, None, 0.1),
    9: (0.042, 0.06, 1, 0.97, None, 0.0), 10: (0.100, 0.10, 2, 0.93, None, 0.2), 11: (0.060, 0.12, 8, 0.94, None, 0.3),
    12: (0.000, 0.01, 0, 1.06, None, 0.0), 13: (0.035, 0.05, 1, 0.98, "n", 0.0), 14: (0.035, 0.05, 1, 0.98, "s", 0.0),
    15: (0.050, 0.09, 2, 0.96, None, 0.8),
}


def laylight():
    N = CELL * 4
    alb = np.zeros((N, N, 3), np.float32)
    nrm = np.zeros((N, N, 3), np.float32)
    msk = np.ones((N, N, 4), np.float32)
    emi = np.zeros((N, N, 3), np.float32)
    jj, ii = np.meshgrid(np.arange(CELL, dtype=np.float32), np.arange(CELL, dtype=np.float32))
    u = (jj + 0.5) / CELL                    # 0..1 across the panel (module X)
    v = 1.0 - (ii + 0.5) / CELL              # 0..1 up the panel (module Z); image rows run down
    xm, ym = (u - 0.5) * PANEL_MM, (v - 0.5) * PANEL_MM
    edge = np.minimum(np.minimum(u, 1 - u), np.minimum(v, 1 - v)) * PANEL_MM
    for cell, (yel, dust_amt, insects, gain, dead, dusty_edge) in CELLS.items():
        seed = 4400 + 31 * cell
        rng = np.random.default_rng(seed)
        dust = spectral(CELL, CELL, 1.4, seed) * 0.6 + spectral(CELL, CELL, 2.4, seed + 1) * 0.4
        # two lamp rows along u at v 0.25 and 0.75 (tubes 300 mm apart, 0.5 m above in the cavity: wide soft bands)
        rows = []
        for (vc, tag) in ((0.25, "s"), (0.75, "n")):
            band = np.exp(-((v - vc) / 0.13) ** 2) * (0.92 + 0.08 * np.exp(-((v - vc) / 0.045) ** 2))
            if dead == tag:
                band = band * 0.10
            rows.append(band)
        E = 0.66 + 0.34 * (rows[0] + rows[1])
        # the T-bar webs and the cavity's corners: the panel's edge dimmer, its corners more so
        E *= 0.74 + 0.26 * smooth(0.0, 70.0, edge)
        E *= 1.0 - 0.10 * smooth(0.6, 0.0, np.minimum(np.abs(u - 0.5), np.abs(v - 0.5)) * 2 - 0.4) * 0.0
        E *= gain * (1.0 - 0.35 * dust_amt * (dust - 0.3))
        if dusty_edge > 0:
            E *= 1.0 - 0.18 * dusty_edge * (1 - smooth(10.0, 120.0, edge))
        a = hexc("#E9E8E1") * (1 - dust_amt * 0.9 * dust[..., None])
        tint = np.array([1.0, 1.0 - 0.35 * yel, 1.0 - 1.0 * yel], np.float32)
        a = a * tint
        # insects and debris lying on the panel: dark silhouettes against the light
        for _ in range(insects):
            cx, cy = rng.uniform(-250, 250), rng.uniform(-250, 250)
            ang = rng.uniform(0, math.pi)
            dx, dy = xm - cx, ym - cy
            ru = dx * math.cos(ang) + dy * math.sin(ang)
            rv = -dx * math.sin(ang) + dy * math.cos(ang)
            size = rng.uniform(3.0, 8.0)
            body = np.exp(-((ru / (size * 1.6)) ** 2 + (rv / size) ** 2) * 1.8)
            E *= 1 - 0.9 * body
            a = a * (1 - 0.85 * body[..., None])
        # a gentle sag of the panel (the normal tips outward from the middle), no prisms here (the detail map)
        gx = xm / (PANEL_MM / 2) * 0.012
        gy = ym / (PANEL_MM / 2) * 0.012
        n = normal_from_grad(gx, gy)
        e_rgb = E[..., None] * tint[None, None, :] ** 0.5
        cx0, cy0 = (cell % 4) * CELL, (cell // 4) * CELL
        r0 = N - cy0 - CELL
        alb[r0:r0 + CELL, cx0:cx0 + CELL] = a
        nrm[r0:r0 + CELL, cx0:cx0 + CELL] = n
        msk[r0:r0 + CELL, cx0:cx0 + CELL, 0] = 0.72 - 0.9 * dust_amt * dust
        msk[r0:r0 + CELL, cx0:cx0 + CELL, 1] = 1.0
        msk[r0:r0 + CELL, cx0:cx0 + CELL, 2] = 0.0
        emi[r0:r0 + CELL, cx0:cx0 + CELL] = e_rgb
    save("Prop_LaylightDiffuser_A", alb, "RGB")
    save("Prop_LaylightDiffuser_N", nrm * 0.5 + 0.5, "RGB")
    save("Prop_LaylightDiffuser_S", msk, "RGBA")
    save("Prop_LaylightDiffuser_E", lin_to_srgb(np.clip(emi, 0, 1)), "RGB")
    save("Prop_LaylightDiffuser_Phone_A", down2(alb), "RGB")
    nh = down2(nrm)
    nh /= np.linalg.norm(nh, axis=-1, keepdims=True)
    save("Prop_LaylightDiffuser_Phone_N", nh * 0.5 + 0.5, "RGB")
    save("Prop_LaylightDiffuser_Phone_S", down2(msk), "RGBA")
    save("Prop_LaylightDiffuser_Phone_E", lin_to_srgb(np.clip(down2(emi), 0, 1)), "RGB")
    # the detail map: square cone prisms, 4 a span (1 in), slopes +-0.12 (x _DetailKind.z 3 in the material)
    D = 512
    jj, ii = np.meshgrid(np.arange(D, dtype=np.float32), np.arange(D, dtype=np.float32))
    pu = ((jj + 0.5) / D * 4.0) % 1.0
    pv = ((ii + 0.5) / D * 4.0) % 1.0
    # a pyramid: the facet facing the larger offset from the centre wins
    du, dv = pu - 0.5, pv - 0.5
    on_u = np.abs(du) >= np.abs(dv)
    su = np.where(on_u, np.sign(du) * -0.12, 0.0)
    sv = np.where(~on_u, np.sign(dv) * 0.12, 0.0)
    ridge = np.exp(-((np.abs(np.abs(du) - np.abs(dv))) / 0.02) ** 2)
    det = np.zeros((D, D, 4), np.float32)
    det[..., 0] = 0.5 + su * 4.0
    det[..., 1] = 0.5 + sv * 4.0
    det[..., 2] = 0.5 + 0.08 * ridge
    det[..., 3] = 0.5 + 0.04 * ridge
    save("Prop_LaylightDiffuser_D", det, "RGBA")


# ============================================================================ FRP grating
def frp_grating():
    N = 512
    base = hexc("#9C9E99")
    tone = spectral(N, N, 1.8, 51)
    grit = spectral(N, N, 0.2, 52)
    grains = (grit > 0.72).astype(np.float32) * (grit - 0.72) / 0.28
    a = base * (0.90 + 0.12 * tone[..., None]) * (1 + 0.20 * grains[..., None])
    h = grit * 0.6 + spectral(N, N, 0.6, 53) * 0.4
    gx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) * 2.2
    gy = (np.roll(h, 1, 0) - np.roll(h, -1, 0)) * 2.2
    n = normal_from_grad(gx, gy)
    m = np.ones((N, N, 4), np.float32)
    m[..., 0] = 0.30 - 0.15 * grains
    m[..., 1] = 1.0 - 0.25 * (1 - grit)
    m[..., 2] = 0.0
    save("Prop_FRPGrating_A", a, "RGB")
    save("Prop_FRPGrating_N", n * 0.5 + 0.5, "RGB")
    save("Prop_FRPGrating_S", m, "RGBA")


# ============================================================================ flexible conduit
def flex_conduit():
    W, H = 256, 128
    jj, ii = np.meshgrid(np.arange(W, dtype=np.float32), np.arange(H, dtype=np.float32))
    u = (jj + 0.5) / W * 4.0            # four convolutions along u
    ph = u % 1.0
    # the interlocked strip: a raised crown, a steep lock shoulder, a groove (height in mm over a 9 mm pitch)
    hgt = np.where(ph < 0.62, np.sin(np.clip(ph / 0.62, 0, 1) * math.pi) * 0.9, -0.55 * np.sin(np.clip((ph - 0.62) / 0.38, 0, 1) * math.pi))
    dh = np.gradient(hgt, axis=1) * (W / 36.0)        # per mm (36 mm a tile)
    n = normal_from_grad(dh * 1.4, np.zeros_like(dh))
    spang = spectral(H, W, 1.0, 61)
    a = hexc("#A7AAA6") * (0.86 + 0.18 * spang[..., None]) * (0.80 + 0.20 * smooth(-0.5, 0.6, hgt)[..., None])
    m = np.ones((H, W, 4), np.float32)
    m[..., 0] = 0.52 + 0.12 * spang
    m[..., 1] = 0.55 + 0.45 * smooth(-0.55, 0.3, hgt)
    m[..., 2] = 1.0
    save("Prop_FlexConduit_A", a, "RGB")
    save("Prop_FlexConduit_N", n * 0.5 + 0.5, "RGB")
    save("Prop_FlexConduit_S", m, "RGBA")


if __name__ == "__main__":
    laylight()
    frp_grating()
    flex_conduit()
    print("G1B TEX DONE", OUT)
