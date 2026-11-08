"""FRONTROOMSSS structures lamps: lens maps for ZL_FLUSH and ZL_GLOBE (P7 structures_lamp_common, G1-b, 2026-11-06).

Run: venv/bin/python zl_struct_tex.py <layout dir (zl_struct.py out)> <tex out dir>
House style of zone_lamps/kit/gen_lamp_textures.py (its helpers are reused: Canvas, normal_from_grad, spectral, band):
  _A sRGB albedo, _N linear normal (OpenGL +Y), _S linear mask (R smoothness, G cavity, B metal, A 1),
  _E emission stored sRGB-encoded (the GPU decodes the linear value painted here). Emission is relative (peak 1.0); the
  lamp's level and colour come from _EmissionColor through FrontRoomsLampKit's lens drive, so off / dim / dead render.
No text, label, brand or logo is drawn. ASCII only.
"""
import json
import math
import os
import sys

import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
LAYDIR, TEXOUT = sys.argv[1], sys.argv[2]
os.makedirs(TEXOUT, exist_ok=True)
_argv = sys.argv
sys.argv = [_argv[0], LAYDIR, TEXOUT]
_src = open(os.path.join(HERE, "gen_lamp_textures.py")).read()
exec(compile(_src[:_src.index("\ndef main():")], "gen_lamp_textures_helpers", "exec"))
sys.argv = _argv
OUT = TEXOUT


def p_swatch(cv, r):
    paint_swatch(cv, r, r["finish"], "enamel")


def p_flushface(cv, r, seed):
    """A 2 x 2 ft prismatic acrylic lens (cone prisms 1/4 in pitch) over two U-lamps: four legs faint through it, their
    bends at one end, the edges under the frame dimmer; a little dust and two dead insects; slightly yellowed."""
    rows, cols, a, b = cv.grid(r)
    L = 563.0
    x = (a - 0.5) * L
    y = (b - 0.5) * L
    pitch = 6.35
    px, py = (x / pitch) % 1.0, (y / pitch) % 1.0
    # square pyramid prisms: slopes +-0.5 in each quarter (the normal map carries them; the detail fades them far off)
    gx = np.where(px < 0.5, 0.45, -0.45) * (np.abs(py - 0.5) < 0.5)
    gy = np.where(py < 0.5, 0.45, -0.45)
    nrm = normal_from_grad(gx * 0.35, gy * 0.35)
    legs = [-195.0, -45.0, 45.0, 195.0]
    E = np.zeros_like(x)
    # the straight legs run to y 190, where each pair turns in its U bend (a half circle of 75 mm round x -120 / +120);
    # the socket end (-y) dims over the last 40 mm (the cathodes)
    for lx in legs:
        E += np.exp(-((x - lx) / 52.0) ** 2) * (1 - smooth(170.0, 215.0, y)) * (1 - 0.65 * smooth(225.0, 262.0, -y))
    for (cxl, half) in ((-120.0, 75.0), (120.0, 75.0)):
        rr = np.hypot(x - cxl, np.maximum(y - 190.0, 0.0))
        E += np.exp(-((rr - half) / 52.0) ** 2) * smooth(150.0, 215.0, y)
    E = 0.50 + 0.36 * np.clip(E, 0.0, 1.2)
    edge = np.minimum(L / 2 - np.abs(x), L / 2 - np.abs(y))
    E *= 0.62 + 0.38 * smooth(4.0, 55.0, edge)
    dust = spectral(*a.shape, 1.6, seed)
    E *= 0.95 + 0.07 * (dust - 0.5)
    alb = hexc("#E6E5DC") * (1.0 - 0.05 * dust[..., None])
    rng = np.random.default_rng(seed)
    for _ in range(2):
        cx, cy = rng.uniform(-220, 220), rng.uniform(-220, 220)
        d = np.hypot(x - cx, (y - cy) * 1.8)
        spot = np.exp(-(d / 4.5) ** 2)
        E *= 1 - 0.85 * spot
        alb = alb * (1 - 0.8 * spot[..., None])
    cv.put(r, alb=alb, nrm=nrm, sm=0.74, cav=1.0, met=0.0, emi=np.clip(E, 0, 1))


def p_jar(cv, r, seed):
    """The jelly jar's clear glass round a frosted 100 W A19: u round the jar, v from the collar (0) to the bottom pole (1).
    The lamp is a hot band over the middle, its tip glowing into the dome; mould seams at u 0.25 and 0.75; water and dead
    insects in the bottom (the age the research names)."""
    rows, cols, a, b = cv.grid(r)
    core = np.exp(-((b - 0.50) / 0.20) ** 2)
    tip = np.exp(-((b - 0.86) / 0.10) ** 2) * 0.45
    E = 0.16 + 0.80 * core + tip
    E = np.where(b < 0.06, E * 0.4, E)
    seam = np.exp(-((np.minimum(np.abs(a - 0.25), np.abs(a - 0.75)) / 0.006) ** 2))
    gx = (np.gradient(seam, axis=1) * 6.0)
    nrm = normal_from_grad(gx, np.zeros_like(gx))
    rng = np.random.default_rng(seed)
    dirt = np.zeros_like(a)
    for _ in range(9):
        cu, cvv = rng.uniform(0, 1), rng.uniform(0.90, 0.99)
        d = np.hypot((a - cu) * 6.0, (b - cvv) * 1.0)
        dirt = np.maximum(dirt, np.exp(-(d / 0.012) ** 2))
    water = smooth(0.93, 0.975, b) * 0.35
    alb = hexc("#A9B2AE") * (1 - 0.15 * water[..., None]) * (1 - 0.6 * dirt[..., None]) + hexc("#2A2216") * (0.6 * dirt[..., None])
    E = E * (1 - 0.7 * dirt) * (1 - 0.25 * water)
    cv.put(r, alb=np.clip(alb, 0, 1), nrm=nrm, sm=0.93, cav=1.0, met=0.0, emi=np.clip(E, 0, 1))


def p_jarcap(cv, r, seed):
    cv.put(r, alb=hexc("#30302E"), sm=0.3, cav=1.0, met=0.0, emi=0.0)


PAINT = dict(flushface=p_flushface, jar=p_jar, jarcap=p_jarcap)


def paint_kit(kid):
    path = os.path.join(LAYDIR, "ZL_%s_layout.json" % kid)
    lay = json.load(open(path))
    A = lay["atlas"]
    cv = Canvas(*A["size"])
    for i, (n, r) in enumerate(sorted(A["regions"].items())):
        k = r.get("kind")
        if k == "swatch":
            p_swatch(cv, r)
            continue
        PAINT[k](cv, r, 8300 + 37 * i)
    cv.dilate(10)
    cv.write("ZL_%s_Lens" % kid, half_as=False, emission=True)
    print("TEX", kid, A["size"])


for kid in ("FLUSH", "GLOBE"):
    paint_kit(kid)
