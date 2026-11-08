#!/usr/bin/env python3
# FrontRooms pool tile generator. Game visual line, the pool family's look (workflow W/pool_look, stage "Tile").
# Doc: Frontrooms3D/Documentation/research/bathroom_rooms/70_look_build.md, section "4. Tile".
# ASCII only. Python 3 + numpy + Pillow (W/venv). Deterministic: fixed seeds, same files every run.
"""
Writes the pool family's tile maps for FrontRooms/Surface's _FR_TILE layer, and the materials that use them.

Usage (from the project root):
  python Tools/lookdev/pool_tile/gen_pool_tile.py [--out Assets/Resources/Surfaces/Pool] [--sets Field,Mosaic,...]
                                                 [--preview <dir>] [--no-materials]

For every set S, into <out>/Textures/ (all maps seamless along U; the square sets along V too):
  PoolTile_<S>_D.png   data, linear. R tone: albedo multiplier m = 0.75 + 0.5 R (glaze lot, mottle and the rounded
                       edge's shading on the tile; grain and recess shading on the grout: relief goes into tone, since
                       1 px is about 2 mm at 3 m). G glaze thickness (1 full; low on the cushion edge, where the body
                       shows through: the shader blends _EdgeColor -> _BaseColor by it). B grout coverage (0 tile, 1 grout).
  PoolTile_<S>_N.png   normal, OpenGL (+Y = +V), in the surface's own UV frame (world-planar or mesh UV in metres).
  PoolTile_<S>_S.png   mask, linear. R smoothness, G cavity, B dirt potential (grout, crazing lines, edges; shown only as
                       far as _TileWear), A chips (glaze knocked off to the body; shown only as far as _TileWear).
  ..._Phone.png        the same maps at half size (the phone tier: handheld and WebGL, FrontRoomsTrofferKit.PhoneTier).
  PoolTile_Face_F.png  the real look's face map (70 section 13, make_face): the glaze's waviness and the grout's grain as slopes,
                       no tile structure (each tile reads its own window of it); every material's _TileFaceMap.
Each PNG gets a .meta (an existing .meta keeps its GUID). Materials Prop_PoolTile_<M>.mat and _Phone.mat go to <out>
(same rule for GUIDs). <out>/Textures/pool_tile_manifest.json lists every file with size, sha1 and GUID.

Scale (Red's rule of thumb for walls: 1 px is about 2 mm at 3 m, so relief also goes into tone):
  Field   glazed wall tile 150 mm (6 in), joint 3 mm (1/8 in), cushion edge r 1.8 mm; map 1.2 m = 8 x 8 tiles
  Mosaic  unglazed porcelain 25 mm (1 in), joint 2 mm, on 300 mm face-mounted sheets; map 0.6 m = 24 x 24 chips
  Deck    unglazed abrasive (silicon carbide grit) 150 mm, joint 4 mm; map 1.2 m = 8 x 8
  Cap     the wainscot's top course: one 150 mm row, bullnose r 9 mm at the top; map 1.2 x 0.15 m
  Coping  full bullnose coping, 300 mm units, unrolled profile 0.34 m (mesh UV: back 30, top 255, nose 47, under 5 mm)
  Nosing  step nosing L: 50 mm on the tread (3 safety ribs), r 8 mm round, 25 mm down the riser; 150 mm units; unrolled
          profile 0.075 m (mesh UV: flat 42, round 12.57, flat 17 mm)
Every period divides 192 m (the room stream's origin rebase), so a rebase never moves a joint.

Structures assets (scene 4, research/structures_assets/30_plan.md package P2 structures_tile_trims, 2026-11-06), mesh UV in metres:
  Field50 glazed 2 in tile (50 mm, 12 to 0.6 m), joint 1.6 mm (1/16 in), cushion r 1.0; map 0.6 m at 1024 px (0.59 mm/px)
  Bullnose the outside-corner bullnose trim: a 142 mm leg down the face, r 8 round, a 142 mm leg on the top; 150 mm units;
          unrolled profile 0.30 m (P1 FrontRoomsStructureProfiles.BullnoseCorner: V 0-0.142 face, round, 0.1546-0.2966 top)
  Cove    the pool's cove r 50 at a basin's floor with 50 mm flats (P1 Cove(0.05, 0.05)): floor flat 0-0.05, round 0.05-0.128,
          wall flat 0.128-0.178; 150 mm units; unrolled 0.18 m
  Gutter  the roll-out gutter (P1 GutterSection(RollOut)): deck 0-0.20 (abrasive), the flush porcelain finger grate 0.20-0.40
          (12 mm fingers, 8 mm slots, dark channel under), the curb's back 0.40-0.45, its bullnosed top 0.45-0.5426, the pool face
          0.5426-0.6926; 150 mm units; unrolled 0.70 m
  WeirLip a square porcelain weir lip with a drip groove (P1 WeirLip): underside 0-0.035, the groove 0.035-0.053, the underside
          to the nose 0.053-0.062, the nose face 0.062-0.122, the top 0.122-0.272 (the water's sheen line at the nose); 150 mm
          units; unrolled 0.28 m
and the mesh-UV kinds for tiled curves (Red's ask 2, tile by radius): Field108Mesh and Field75Mesh are the Field set at 109.1 mm
(11 to 1.2 m) and 75 mm, Field50Mesh the 2 in set, MosaicMesh the 1 in mosaic on mesh UVs.
"""
import argparse
import hashlib
import json
import os
import re
import uuid

import numpy as np
from PIL import Image

SS = 2                      # supersampling over the desktop map (anti-aliased joints)
SHADER_GUID = "38dd6d7df75f84369b8237ac8848106c"   # Assets/Resources/Rendering/FrontRoomsSurface.shader
MACRO_GUID = "f777b2e3dd7d240d390e2b2cd59cbb9e"    # the shared macro wear map every Surface material uses


# ------------------------------------------------------------------------------------------------ noise and filters
def _freq(h, w):
    fy = np.fft.fftfreq(h)[:, None].astype(np.float32)
    fx = np.fft.rfftfreq(w)[None, :].astype(np.float32)
    return np.sqrt(fx * fx + fy * fy)


def noise(h, w, lam_px, seed, width=0.45):
    """Periodic band noise around wavelength lam_px (pixels), Gaussian in log-frequency, unit std."""
    r = np.random.default_rng(seed)
    F = np.fft.rfft2(r.standard_normal((h, w)).astype(np.float32))
    f = _freq(h, w)
    f0 = 1.0 / max(lam_px, 1.0)
    with np.errstate(divide="ignore"):
        lf = np.log(np.maximum(f, 1e-9) / f0)
    g = np.exp(-0.5 * (lf / width) ** 2).astype(np.float32)
    g[0, 0] = 0.0
    n = np.fft.irfft2(F * g, s=(h, w)).astype(np.float32)
    s = float(n.std())
    return n / s if s > 0 else n


def blur(a, sigma_px):
    """Periodic Gaussian blur."""
    if sigma_px <= 0:
        return a
    h, w = a.shape
    f = _freq(h, w)
    g = np.exp(-2.0 * (np.pi ** 2) * (sigma_px ** 2) * f * f).astype(np.float32)
    return np.fft.irfft2(np.fft.rfft2(a) * g, s=(h, w)).astype(np.float32)


def smoothstep(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


def worley_f2f1(h, w, cell, seed):
    """Periodic cellular noise: F2 - F1 in pixels (small on the borders between cells). cell must divide h and w."""
    r = np.random.default_rng(seed)
    gh, gw = h // cell, w // cell
    jx = r.random((gh, gw)).astype(np.float32)
    jy = r.random((gh, gw)).astype(np.float32)
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    yy += 0.5
    xx += 0.5
    cy = (yy // cell).astype(np.int32)
    cx = (xx // cell).astype(np.int32)
    f1 = np.full((h, w), 1e9, np.float32)
    f2 = np.full((h, w), 1e9, np.float32)
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            ny = (cy + dy) % gh
            nx = (cx + dx) % gw
            px = (cx + dx + jx[ny, nx]) * cell
            py = (cy + dy + jy[ny, nx]) * cell
            d = np.hypot(xx - px, yy - py)
            f2 = np.where(d < f1, f1, np.minimum(f2, d))
            f1 = np.minimum(f1, d)
    return f2 - f1


def down(a, k):
    """Box downsample by k along both axes."""
    if k == 1:
        return a
    h, w = a.shape[:2]
    if a.ndim == 2:
        return a.reshape(h // k, k, w // k, k).mean(axis=(1, 3))
    return a.reshape(h // k, k, w // k, k, a.shape[2]).mean(axis=(1, 3))


def normals(hgt, mm_u, mm_v, wrap_v=True, max_slope=6.0):
    """Unit normals (H, W, 3) from a height field in mm (rows = +V upward), OpenGL convention."""
    dhdu = (np.roll(hgt, -1, axis=1) - np.roll(hgt, 1, axis=1)) / (2.0 * mm_u)
    if wrap_v:
        dhdv = (np.roll(hgt, -1, axis=0) - np.roll(hgt, 1, axis=0)) / (2.0 * mm_v)
    else:
        dhdv = np.gradient(hgt, axis=0) / mm_v
    g = np.sqrt(dhdu * dhdu + dhdv * dhdv)
    k = np.where(g > max_slope, max_slope / np.maximum(g, 1e-9), 1.0)
    n = np.stack([-dhdu * k, -dhdv * k, np.ones_like(hgt)], axis=-1)
    n /= np.linalg.norm(n, axis=-1, keepdims=True)
    return n.astype(np.float32)


# ------------------------------------------------------------------------------------------------ the square tile sheet
class Sheet(object):
    """Fields of one map at SS x the desktop resolution; rows run along +V (row 0 = V 0, flipped when saved)."""

    def __init__(self, name, wd, hd, span_u, span_v):
        self.name, self.wd, self.hd = name, wd, hd
        self.W, self.H = wd * SS, hd * SS
        self.span_u, self.span_v = float(span_u), float(span_v)
        self.mm_u, self.mm_v = self.span_u / self.W, self.span_v / self.H
        xs = (np.arange(self.W, dtype=np.float32) + 0.5) * self.mm_u
        ys = (np.arange(self.H, dtype=np.float32) + 0.5) * self.mm_v
        self.X, self.Y = np.meshgrid(xs, ys)
        z = np.zeros((self.H, self.W), np.float32)
        self.hgt = z.copy()        # mm, 0 = the tile face
        self.tone = np.ones_like(z)
        self.thick = np.ones_like(z)
        self.grout = z.copy()
        self.smooth = z.copy()
        self.cavity = np.ones_like(z)
        self.dirt = z.copy()
        self.chips = z.copy()
        self.normal_override = None
        self.wrap_v = True
        self.params = {}

    def px(self, mm):
        return mm / self.mm_u


def grid_cells(sh, pitch_u, pitch_v, joint, corner_r, jitter=None):
    """Tile cells on a square grid: per pixel the tile id, centred local coordinates (mm) and the signed distance to the
    tile face's rounded-rectangle edge (mm, + inside the face). jitter(iu, iv) -> (du, dv) shifts a tile in its cell."""
    nu = max(1, int(round(sh.span_u / pitch_u)))
    nv = max(1, int(round(sh.span_v / pitch_v)))
    iu = np.clip((sh.X // pitch_u).astype(np.int32), 0, nu - 1)
    iv = np.clip((sh.Y // pitch_v).astype(np.int32), 0, nv - 1)
    lx = sh.X - (iu + 0.5) * pitch_u
    ly = sh.Y - (iv + 0.5) * pitch_v
    if jitter is not None:
        du, dv = jitter(iu, iv)
        lx = lx - du
        ly = ly - dv
    hu = pitch_u * 0.5 - joint * 0.5
    hv = pitch_v * 0.5 - joint * 0.5
    qx = np.abs(lx) - (hu - corner_r)
    qy = np.abs(ly) - (hv - corner_r)
    outside = np.hypot(np.maximum(qx, 0), np.maximum(qy, 0)) + np.minimum(np.maximum(qx, qy), 0) - corner_r
    d = -outside
    tid = iv * nu + iu
    return dict(nu=nu, nv=nv, iu=iu, iv=iv, lx=lx, ly=ly, d=d, tid=tid, hu=hu, hv=hv)


def tile_surface(sh, g, rng, edge_r, g_edge, g_dip, lip_sd, tilt_sd_deg, bow_lo, bow_hi, saddle_sd, joint):
    """Height of tiles and grout (mm): per-tile lippage, tilt, convex bow and saddle; the cushion edge (quarter circle of
    edge_r); the grout struck concave (g_edge below the face at the tile, g_edge + g_dip at the joint's centre)."""
    n = g["nu"] * g["nv"]
    lip = rng.normal(0.0, lip_sd, n).astype(np.float32)
    t = np.tan(np.radians(tilt_sd_deg))
    tx = rng.normal(0.0, t, n).astype(np.float32)
    ty = rng.normal(0.0, t, n).astype(np.float32)
    bow = rng.uniform(bow_lo, bow_hi, n).astype(np.float32)
    sad = rng.normal(0.0, saddle_sd, n).astype(np.float32)
    tid, lx, ly, d = g["tid"], g["lx"], g["ly"], g["d"]
    nx = lx / g["hu"]
    ny = ly / g["hv"]
    face = lip[tid] + tx[tid] * lx + ty[tid] * ly + bow[tid] * (1.0 - 0.5 * (nx * nx + ny * ny)) + sad[tid] * nx * ny
    dd = np.clip(d, 0.0, edge_r)
    edge = np.sqrt(np.maximum(edge_r * edge_r - (edge_r - dd) ** 2, 0.0)) - edge_r
    edge = np.where(d < 0.0, -edge_r - 0.5 * np.minimum(-d, 1.0), edge)
    h_tile = face + edge
    half = joint * 0.5
    c = np.clip((d + half) / half, 0.0, 1.0)
    h_grout = -(g_edge + g_dip * (1.0 - c * c))
    h_grout = np.where(d > 0.0, -g_edge, h_grout)
    grout = (h_grout >= h_tile).astype(np.float32)
    hgt = np.maximum(h_tile, h_grout)
    shoulder = np.clip(-edge / edge_r, 0.0, 1.0) * (1.0 - grout)
    depth = np.clip((-h_grout - g_edge) / max(g_dip, 1e-6), 0.0, 1.0)
    return dict(hgt=hgt, grout=grout, shoulder=shoulder, depth=depth, face=face, d=d)


def chips_mask(sh, g, rng, count, r_lo, r_hi, trim=False):
    """Bites out of the face edge: count chips on random tiles and edges, radius r_lo..r_hi mm. A trim (one row whose
    face runs off the map along V) is chipped only at its joints along U."""
    m = np.zeros((sh.H, sh.W), np.float32)
    n = g["nu"] * g["nv"]
    pu = sh.span_u / g["nu"]
    pv = sh.span_v / g["nv"]
    for k in range(count):
        t = int(rng.integers(0, n))
        iu, iv = t % g["nu"], t // g["nu"]
        side = int(rng.integers(0, 2 if trim else 4))
        r = float(rng.uniform(r_lo, r_hi))
        along = float(rng.uniform(-0.42, 0.42))
        cx = (iu + 0.5) * pu
        cy = (iv + 0.5) * pv
        if trim:
            cy = float(rng.uniform(0.15, 0.85)) * sh.span_v
            along = 0.0
        if side == 0:
            cx += g["hu"] + r * 0.35; cy += along * pv
        elif side == 1:
            cx -= g["hu"] + r * 0.35; cy += along * pv
        elif side == 2:
            cy += g["hv"] + r * 0.35; cx += along * pu
        else:
            cy -= g["hv"] + r * 0.35; cx += along * pu
        dx = sh.X - cx
        dy = sh.Y - cy
        # an irregular conchoidal bite: radius wobbles with the angle
        ang = np.arctan2(dy, dx)
        rr = r * (1.0 + 0.18 * np.sin(3.0 * ang + k) + 0.10 * np.sin(5.0 * ang + 2 * k))
        inside = (np.hypot(dx, dy) < rr) & (g["tid"] == t) & (g["d"] > 0.0)
        m = np.maximum(m, inside.astype(np.float32))
    return m


def crazing(sh, g, rng, share, cell_mm, line_mm, seed):
    """Crazing in the glaze: a fine polygonal crack network on a share of the tiles (dirt-filled over the years)."""
    cell = int(round(cell_mm / sh.mm_u))
    while sh.W % cell or sh.H % cell:
        cell += 1
    f = worley_f2f1(sh.H, sh.W, cell, seed)
    line = 1.0 - smoothstep(0.0, line_mm / sh.mm_u, f)
    n = g["nu"] * g["nv"]
    on = (rng.random(n) < share).astype(np.float32)
    strength = rng.uniform(0.45, 1.0, n).astype(np.float32)
    return line * on[g["tid"]] * strength[g["tid"]] * smoothstep(0.6, 2.5, g["d"])


# ------------------------------------------------------------------------------------------------ the sets
def make_field(name="Field", wd=2048, seed=150):
    """Glazed porcelain-white wall tile, 150 mm, 3 mm joint, cushion edge."""
    rng = np.random.default_rng(seed)
    sh = Sheet(name, wd, wd, 1200.0, 1200.0)
    P, J = 150.0, 3.0
    g = grid_cells(sh, P, P, J, corner_r=1.2)
    s = tile_surface(sh, g, rng, edge_r=1.8, g_edge=0.7, g_dip=0.6, lip_sd=0.10, tilt_sd_deg=0.10,
                     bow_lo=0.05, bow_hi=0.20, saddle_sd=0.04, joint=J)
    H, W = sh.H, sh.W
    glaze = 1.0 - s["grout"]
    # glaze waviness: orange peel (2.5 mm) and a longer swell (12 mm); it is what makes a lamp's reflection wobble
    peel = noise(H, W, sh.px(2.5), seed + 1) * 0.0045 + noise(H, W, sh.px(12.0), seed + 2) * 0.010
    grain = noise(H, W, sh.px(0.6), seed + 3)
    sh.hgt = s["hgt"] + peel * glaze * smoothstep(0.0, 1.8, s["d"]) + grain * 0.05 * s["grout"]
    # tone: per-tile lot (small: the shader adds the non-repeating lots), mottle, the cushion edge darker
    n = g["nu"] * g["nv"]
    lot = rng.normal(0.0, 0.006, n).astype(np.float32)
    mottle = noise(H, W, sh.px(25.0), seed + 4) * 0.004
    m_glaze = (1.0 + lot[g["tid"]]) * (1.0 + mottle) * (1.0 - 0.045 * s["shoulder"])
    gr2 = noise(H, W, sh.px(1.6), seed + 5)
    m_grout = (1.0 + 0.10 * grain + 0.05 * gr2) * (0.95 - 0.12 * s["depth"])
    sh.tone = np.where(s["grout"] > 0.5, m_grout, m_glaze)
    sh.thick = np.where(s["grout"] > 0.5, 1.0, 0.45 + 0.55 * smoothstep(0.0, 2.2, s["d"]))
    sh.grout = s["grout"]
    sh.smooth = np.where(s["grout"] > 0.5, 0.24 + 0.05 * grain, 0.935 + 0.006 * noise(H, W, sh.px(30.0), seed + 6) - 0.035 * s["shoulder"])
    sh.cavity = np.where(s["grout"] > 0.5, 0.80 - 0.22 * s["depth"], 1.0 - 0.08 * s["shoulder"])
    # dirt potential: grout (patchy, the horizontal joints' ledges a little more), crazing lines, the edges, specks
    patch = np.clip(0.5 + 0.5 * noise(H, W, sh.px(60.0), seed + 7), 0.0, 1.0)
    horiz = (np.abs(g["ly"]) > np.abs(g["lx"]) * (g["hv"] / g["hu"])).astype(np.float32)
    dg = np.clip(0.38 + 0.35 * patch + 0.12 * grain + 0.10 * horiz, 0.0, 1.0)
    craze = crazing(sh, g, rng, share=0.35, cell_mm=9.0, line_mm=0.32, seed=seed + 8)
    speck = (noise(H, W, sh.px(0.8), seed + 9) > 3.1).astype(np.float32)
    sh.dirt = np.where(s["grout"] > 0.5, dg, np.clip(0.62 * craze + 0.12 * s["shoulder"] + 0.35 * speck, 0.0, 1.0))
    sh.chips = chips_mask(sh, g, rng, count=7, r_lo=0.9, r_hi=2.4)
    sh.params = dict(kind="square", tiles_u=g["nu"], tiles_v=g["nv"], pitch_mm=P, joint_mm=J, edge_r_mm=1.8,
                     span_m=[1.2, 1.2], glaze="bright glaze, smoothness 0.935", grout_recess_mm=[0.7, 1.3])
    return sh


def make_mosaic(name="Mosaic", wd=2048, seed=25):
    """Unglazed porcelain 1 in mosaic on 300 mm face-mounted sheets (12 x 12 chips), 2 mm joints."""
    rng = np.random.default_rng(seed)
    sh = Sheet(name, wd, wd, 600.0, 600.0)
    P, J = 25.0, 2.0
    nsu = 2
    so_u = rng.normal(0.0, 0.25, (nsu * nsu,)).astype(np.float32)
    so_v = rng.normal(0.0, 0.25, (nsu * nsu,)).astype(np.float32)
    co_u = rng.normal(0.0, 0.10, (24 * 24,)).astype(np.float32)
    co_v = rng.normal(0.0, 0.10, (24 * 24,)).astype(np.float32)

    def jit(iu, iv):
        sid = (iv // 12) * nsu + (iu // 12)
        cid = iv * 24 + iu
        return so_u[sid] + co_u[cid], so_v[sid] + co_v[cid]

    g = grid_cells(sh, P, P, J, corner_r=1.0, jitter=jit)
    s = tile_surface(sh, g, rng, edge_r=1.0, g_edge=0.6, g_dip=0.4, lip_sd=0.08, tilt_sd_deg=0.35,
                     bow_lo=0.01, bow_hi=0.05, saddle_sd=0.01, joint=J)
    H, W = sh.H, sh.W
    chip = 1.0 - s["grout"]
    rough = noise(H, W, sh.px(0.7), seed + 1) * 0.008 + noise(H, W, sh.px(2.0), seed + 2) * 0.004
    grain = noise(H, W, sh.px(0.5), seed + 3)
    sh.hgt = s["hgt"] + rough * chip + grain * 0.04 * s["grout"]
    n = g["nu"] * g["nv"]
    lot = rng.normal(0.0, 0.018, n).astype(np.float32)
    mottle = noise(H, W, sh.px(9.0), seed + 4) * 0.010
    m_chip = (1.0 + lot[g["tid"]]) * (1.0 + mottle) * (1.0 - 0.04 * s["shoulder"])
    m_grout = (1.0 + 0.10 * grain) * (0.95 - 0.10 * s["depth"])
    sh.tone = np.where(s["grout"] > 0.5, m_grout, m_chip)
    sh.thick = np.ones((H, W), np.float32)
    sh.grout = s["grout"]
    sh.smooth = np.where(s["grout"] > 0.5, 0.24 + 0.04 * grain, 0.47 + 0.03 * noise(H, W, sh.px(5.0), seed + 5) - 0.05 * s["shoulder"])
    sh.cavity = np.where(s["grout"] > 0.5, 0.80 - 0.20 * s["depth"], 1.0 - 0.06 * s["shoulder"])
    patch = np.clip(0.5 + 0.5 * noise(H, W, sh.px(45.0), seed + 6), 0.0, 1.0)
    sh.dirt = np.where(s["grout"] > 0.5, np.clip(0.40 + 0.35 * patch + 0.12 * grain, 0.0, 1.0), 0.10 * s["shoulder"])
    sh.chips = chips_mask(sh, g, rng, count=10, r_lo=0.5, r_hi=1.2)
    sh.params = dict(kind="square", tiles_u=g["nu"], tiles_v=g["nv"], pitch_mm=P, joint_mm=J, edge_r_mm=1.0,
                     span_m=[0.6, 0.6], sheet_mm=300, glaze="unglazed porcelain, smoothness 0.47")
    return sh


def make_deck(name="Deck", wd=2048, seed=152):
    """Unglazed abrasive deck tile (silicon carbide grit through the body), 150 mm, 4 mm joints."""
    rng = np.random.default_rng(seed)
    sh = Sheet(name, wd, wd, 1200.0, 1200.0)
    P, J = 150.0, 4.0
    g = grid_cells(sh, P, P, J, corner_r=1.5)
    s = tile_surface(sh, g, rng, edge_r=1.4, g_edge=1.0, g_dip=0.5, lip_sd=0.22, tilt_sd_deg=0.12,
                     bow_lo=0.04, bow_hi=0.14, saddle_sd=0.04, joint=J)
    H, W = sh.H, sh.W
    face = 1.0 - s["grout"]
    rough = noise(H, W, sh.px(0.8), seed + 1) * 0.014 + noise(H, W, sh.px(3.0), seed + 2) * 0.006
    gritn = noise(H, W, sh.px(0.45), seed + 3)
    grit = smoothstep(2.0, 2.6, gritn) * face
    grain = noise(H, W, sh.px(0.6), seed + 4)
    sh.hgt = s["hgt"] + rough * face + grit * 0.06 + grain * 0.05 * s["grout"]
    n = g["nu"] * g["nv"]
    lot = rng.normal(0.0, 0.012, n).astype(np.float32)
    mottle = noise(H, W, sh.px(30.0), seed + 5) * 0.014 + noise(H, W, sh.px(4.0), seed + 6) * 0.010
    m_face = (1.0 + lot[g["tid"]]) * (1.0 + mottle) * (1.0 - 0.45 * grit) * (1.0 - 0.04 * s["shoulder"])
    m_grout = (1.0 + 0.10 * grain) * (0.94 - 0.10 * s["depth"])
    sh.tone = np.where(s["grout"] > 0.5, m_grout, m_face)
    sh.thick = np.ones((H, W), np.float32)
    sh.grout = s["grout"]
    sh.smooth = np.where(s["grout"] > 0.5, 0.22 + 0.04 * grain, 0.31 + 0.03 * noise(H, W, sh.px(6.0), seed + 7) + 0.30 * grit)
    sh.cavity = np.where(s["grout"] > 0.5, 0.78 - 0.20 * s["depth"], 1.0 - 0.06 * s["shoulder"] - 0.08 * grit)
    patch = np.clip(0.5 + 0.5 * noise(H, W, sh.px(70.0), seed + 8), 0.0, 1.0)
    pores = np.clip(0.5 + 0.5 * noise(H, W, sh.px(1.2), seed + 9), 0.0, 1.0)
    sh.dirt = np.where(s["grout"] > 0.5, np.clip(0.45 + 0.35 * patch + 0.12 * grain, 0.0, 1.0),
                       np.clip(0.10 + 0.20 * patch * pores + 0.15 * s["shoulder"], 0.0, 1.0))
    sh.chips = chips_mask(sh, g, rng, count=9, r_lo=1.0, r_hi=3.0)
    sh.params = dict(kind="square", tiles_u=g["nu"], tiles_v=g["nv"], pitch_mm=P, joint_mm=J, edge_r_mm=1.4,
                     span_m=[1.2, 1.2], glaze="unglazed abrasive (silicon carbide), smoothness 0.31")
    return sh


def make_cap(name="Cap", wd=2048, seed=151):
    """The wainscot's top course: one 150 mm row of glazed tile whose top edge is a bullnose (r 9 mm) turning back to the
    wall; vertical joints every 150 mm and the joint to the field below at V 0."""
    rng = np.random.default_rng(seed)
    sh = Sheet(name, wd, wd // 8, 1200.0, 150.0)
    sh.wrap_v = False
    P, J, Rb = 150.0, 3.0, 9.0
    # a tall virtual cell so the top edge has no joint: the face runs to the bullnose
    g = grid_cells(sh, P, 2.0 * P, J, corner_r=1.2)
    s = tile_surface(sh, g, rng, edge_r=1.8, g_edge=0.7, g_dip=0.6, lip_sd=0.10, tilt_sd_deg=0.08,
                     bow_lo=0.03, bow_hi=0.10, saddle_sd=0.02, joint=J)
    H, W = sh.H, sh.W
    t = np.clip(150.0 - sh.Y, 0.0, None)
    bull = np.where(t < Rb, np.sqrt(np.maximum(Rb * Rb - (Rb - t) ** 2, 0.0)) - Rb, 0.0)
    glaze = 1.0 - s["grout"]
    peel = noise(H, W, sh.px(2.5), seed + 1) * 0.0045 + noise(H, W, sh.px(12.0), seed + 2) * 0.010
    grain = noise(H, W, sh.px(0.6), seed + 3)
    sh.hgt = s["hgt"] + bull + peel * glaze + grain * 0.05 * s["grout"]
    n = g["nu"] * g["nv"]
    lot = rng.normal(0.0, 0.006, n).astype(np.float32)
    mottle = noise(H, W, sh.px(25.0), seed + 4) * 0.004
    bshade = smoothstep(0.0, Rb, Rb - t) * (t < Rb)
    m_glaze = (1.0 + lot[g["tid"]]) * (1.0 + mottle) * (1.0 - 0.045 * s["shoulder"])
    m_grout = (1.0 + 0.10 * grain) * (0.95 - 0.12 * s["depth"])
    sh.tone = np.where(s["grout"] > 0.5, m_grout, m_glaze)
    sh.thick = np.where(s["grout"] > 0.5, 1.0, (0.45 + 0.55 * smoothstep(0.0, 2.2, s["d"])) * (1.0 - 0.25 * smoothstep(0.75, 1.0, bshade)))
    sh.grout = s["grout"]
    sh.smooth = np.where(s["grout"] > 0.5, 0.24 + 0.05 * grain, 0.935 - 0.035 * s["shoulder"])
    sh.cavity = np.where(s["grout"] > 0.5, 0.80 - 0.22 * s["depth"], 1.0 - 0.08 * s["shoulder"])
    patch = np.clip(0.5 + 0.5 * noise(H, W, sh.px(60.0), seed + 7), 0.0, 1.0)
    craze = crazing(sh, g, rng, share=0.35, cell_mm=9.0, line_mm=0.32, seed=seed + 8)
    # the bullnose's top collects dust (it is a ledge)
    ledge = smoothstep(0.55, 1.0, bshade)
    sh.dirt = np.where(s["grout"] > 0.5, np.clip(0.40 + 0.35 * patch + 0.12 * grain, 0.0, 1.0),
                       np.clip(0.62 * craze + 0.12 * s["shoulder"] + 0.45 * ledge * patch, 0.0, 1.0))
    sh.chips = chips_mask(sh, g, rng, count=3, r_lo=0.9, r_hi=2.2, trim=True)
    sh.params = dict(kind="trim", tiles_u=g["nu"], tiles_v=1, pitch_mm=P, joint_mm=J, bullnose_r_mm=Rb, span_m=[1.2, 0.15])
    return sh


def _profile_sheet(name, wd, hd, span_u, span_v, unit, J, seed):
    rng = np.random.default_rng(seed)
    sh = Sheet(name, wd, hd, span_u, span_v)
    sh.wrap_v = False
    # joints across U only: a cell as tall as the map plus margins
    g = grid_cells(sh, unit, span_v * 4.0, J, corner_r=0.0)
    lx = g["lx"]
    d = g["hu"] - np.abs(lx)        # distance to the joint, along U only
    g["d"] = d
    return sh, g, rng


def make_coping(name="Coping", wd=2048, seed=300):
    """Full bullnose coping, 300 mm units, mesh UV: U along the edge, V across the unrolled profile from the deck side:
    back face 0-30, top 30-285 (sanded glaze), nose 285-332.1 (half round r 15, bright glaze), under 332.1-337.1 mm."""
    sh, g, rng = _profile_sheet(name, wd, wd // 4, 1200.0, 340.0, 300.0, 3.0, seed)
    s = tile_surface(sh, g, rng, edge_r=2.0, g_edge=0.8, g_dip=0.5, lip_sd=0.15, tilt_sd_deg=0.0,
                     bow_lo=0.0, bow_hi=0.02, saddle_sd=0.0, joint=3.0)
    H, W = sh.H, sh.W
    v = sh.Y
    top = ((v >= 30.0) & (v < 285.0)).astype(np.float32)
    nose = ((v >= 285.0) & (v < 332.1)).astype(np.float32)
    face = 1.0 - s["grout"]
    sand = noise(H, W, sh.px(0.55), seed + 1) * 0.016 + noise(H, W, sh.px(1.5), seed + 2) * 0.006
    peel = noise(H, W, sh.px(2.5), seed + 3) * 0.0045
    grain = noise(H, W, sh.px(0.6), seed + 4)
    sh.hgt = s["hgt"] + face * (sand * top + peel * (1.0 - top)) + grain * 0.05 * s["grout"]
    n = g["nu"]
    lot = rng.normal(0.0, 0.006, max(n, 1) * g["nv"]).astype(np.float32)
    mottle = noise(H, W, sh.px(25.0), seed + 5) * 0.004
    m_face = (1.0 + lot[g["tid"]]) * (1.0 + mottle) * (1.0 - 0.045 * s["shoulder"]) * (1.0 - 0.02 * top)
    m_grout = (1.0 + 0.10 * grain) * (0.95 - 0.12 * s["depth"])
    sh.tone = np.where(s["grout"] > 0.5, m_grout, m_face)
    sh.thick = np.where(s["grout"] > 0.5, 1.0, 0.45 + 0.55 * smoothstep(0.0, 2.2, s["d"]))
    sh.grout = s["grout"]
    sh.smooth = np.where(s["grout"] > 0.5, 0.24 + 0.05 * grain, 0.62 * top + 0.92 * (1.0 - top) - 0.03 * s["shoulder"])
    sh.cavity = np.where(s["grout"] > 0.5, 0.78 - 0.22 * s["depth"], 1.0 - 0.08 * s["shoulder"] - 0.05 * top)
    patch = np.clip(0.5 + 0.5 * noise(H, W, sh.px(60.0), seed + 6), 0.0, 1.0)
    backline = 1.0 - smoothstep(0.0, 40.0, v)
    walk = top * smoothstep(150.0, 270.0, v) * patch
    sh.dirt = np.where(s["grout"] > 0.5, np.clip(0.45 + 0.35 * patch + 0.12 * grain, 0.0, 1.0),
                       np.clip(0.30 * backline + 0.18 * walk + 0.12 * s["shoulder"], 0.0, 1.0))
    ch = chips_mask(sh, g, rng, count=6, r_lo=1.2, r_hi=3.0, trim=True)
    sh.chips = ch * (nose + top * smoothstep(240.0, 285.0, v))
    sh.params = dict(kind="profile", tiles_u=g["nu"], tiles_v=1, unit_mm=300.0, joint_mm=3.0, span_m=[1.2, 0.34],
                     profile_mm=dict(back=[0, 30], top=[30, 285], nose=[285, 332.1], under=[332.1, 337.1]),
                     nose_r_mm=15.0, overhang_mm=20.0)
    return sh


def make_nosing(name="Nosing", wd=2048, seed=50):
    """Step nosing L, mesh UV: U along the step, V across the unrolled profile from the back of the tread leg: flat tread
    leg 0-42 (three safety ribs), round 42-54.57 (r 8), flat riser leg 54.57-71.57 mm; seen from above 50 mm on the tread,
    from the front 25 mm down the riser (BH 193). 150 mm units, 3 mm joints; V 71.57-75 is unused (grout)."""
    sh, g, rng = _profile_sheet(name, wd, wd // 16, 1200.0, 75.0, 150.0, 3.0, seed)
    s = tile_surface(sh, g, rng, edge_r=1.6, g_edge=0.7, g_dip=0.5, lip_sd=0.10, tilt_sd_deg=0.0,
                     bow_lo=0.0, bow_hi=0.02, saddle_sd=0.0, joint=3.0)
    H, W = sh.H, sh.W
    v = sh.Y
    tread = (v < 42.0).astype(np.float32)
    rib = np.zeros((H, W), np.float32)
    for c in (10.0, 20.0, 30.0):
        q = np.clip(1.0 - np.abs(v - c) / 1.25, 0.0, 1.0)
        rib = np.maximum(rib, np.sqrt(q))
    # half joints to the tread tile behind (V 0) and the riser tile below (V 71.57)
    endj = np.maximum(1.0 - smoothstep(0.6, 1.8, v), smoothstep(70.0, 71.3, v))
    face = (1.0 - s["grout"]) * (1.0 - endj)
    peel = noise(H, W, sh.px(2.5), seed + 1) * 0.0045
    grit = noise(H, W, sh.px(0.5), seed + 2) * 0.012
    grain = noise(H, W, sh.px(0.6), seed + 3)
    base = s["hgt"] + endj * (np.minimum(s["hgt"], -1.2) - s["hgt"])
    sh.hgt = base - 0.6 * rib + face * (peel + grit * tread) + grain * 0.05 * np.maximum(s["grout"], endj)
    gr = np.maximum(s["grout"], endj)
    n = g["nu"]
    lot = rng.normal(0.0, 0.008, max(n, 1) * g["nv"]).astype(np.float32)
    mottle = noise(H, W, sh.px(20.0), seed + 4) * 0.006
    m_face = (1.0 + lot[g["tid"]]) * (1.0 + mottle) * (1.0 - 0.06 * s["shoulder"]) * (1.0 - 0.10 * rib)
    m_grout = (1.0 + 0.10 * grain) * 0.90
    sh.tone = np.where(gr > 0.5, m_grout, m_face)
    # dark glaze breaks lighter where it thins: on the joints' shoulders and the ribs' crests
    sh.thick = np.where(gr > 0.5, 1.0, (0.45 + 0.55 * smoothstep(0.0, 2.0, s["d"])) * (1.0 - 0.35 * rib))
    sh.grout = gr
    sh.smooth = np.where(gr > 0.5, 0.24 + 0.05 * grain, (0.80 * tread + 0.90 * (1.0 - tread)) - 0.35 * rib - 0.03 * s["shoulder"])
    sh.cavity = np.where(gr > 0.5, 0.78, 1.0 - 0.25 * rib - 0.08 * s["shoulder"])
    patch = np.clip(0.5 + 0.5 * noise(H, W, sh.px(50.0), seed + 5), 0.0, 1.0)
    sh.dirt = np.where(gr > 0.5, np.clip(0.45 + 0.35 * patch, 0.0, 1.0), np.clip(0.55 * rib * patch + 0.12 * s["shoulder"], 0.0, 1.0))
    sh.chips = chips_mask(sh, g, rng, count=4, r_lo=0.8, r_hi=2.0, trim=True) * (1.0 - tread)
    sh.params = dict(kind="profile", tiles_u=g["nu"], tiles_v=1, unit_mm=150.0, joint_mm=3.0, span_m=[1.2, 0.075],
                     profile_mm=dict(tread=[0, 42], round=[42, 54.57], riser=[54.57, 71.57]), round_r_mm=8.0,
                     seen_mm=dict(on_tread=50, down_riser=25), ribs_mm=[10.0, 20.0, 30.0])
    return sh


def make_field50(name="Field50", wd=1024, seed=250):
    """Glazed porcelain-white 2 in tile (50 mm), 1.6 mm joint, cushion r 1.0: the tile for tight curves (R 0.3-0.6 m)."""
    rng = np.random.default_rng(seed)
    sh = Sheet(name, wd, wd, 600.0, 600.0)
    P, J = 50.0, 1.6
    g = grid_cells(sh, P, P, J, corner_r=0.8)
    s = tile_surface(sh, g, rng, edge_r=1.0, g_edge=0.5, g_dip=0.4, lip_sd=0.07, tilt_sd_deg=0.14,
                     bow_lo=0.02, bow_hi=0.08, saddle_sd=0.02, joint=J)
    H, W = sh.H, sh.W
    glaze = 1.0 - s["grout"]
    peel = noise(H, W, sh.px(2.5), seed + 1) * 0.0045 + noise(H, W, sh.px(12.0), seed + 2) * 0.008
    grain = noise(H, W, sh.px(0.6), seed + 3)
    sh.hgt = s["hgt"] + peel * glaze * smoothstep(0.0, 1.0, s["d"]) + grain * 0.04 * s["grout"]
    n = g["nu"] * g["nv"]
    lot = rng.normal(0.0, 0.008, n).astype(np.float32)
    mottle = noise(H, W, sh.px(20.0), seed + 4) * 0.004
    m_glaze = (1.0 + lot[g["tid"]]) * (1.0 + mottle) * (1.0 - 0.045 * s["shoulder"])
    m_grout = (1.0 + 0.10 * grain) * (0.95 - 0.12 * s["depth"])
    sh.tone = np.where(s["grout"] > 0.5, m_grout, m_glaze)
    sh.thick = np.where(s["grout"] > 0.5, 1.0, 0.45 + 0.55 * smoothstep(0.0, 1.4, s["d"]))
    sh.grout = s["grout"]
    sh.smooth = np.where(s["grout"] > 0.5, 0.24 + 0.05 * grain, 0.93 + 0.006 * noise(H, W, sh.px(25.0), seed + 6) - 0.035 * s["shoulder"])
    sh.cavity = np.where(s["grout"] > 0.5, 0.80 - 0.22 * s["depth"], 1.0 - 0.08 * s["shoulder"])
    patch = np.clip(0.5 + 0.5 * noise(H, W, sh.px(45.0), seed + 7), 0.0, 1.0)
    craze = crazing(sh, g, rng, share=0.25, cell_mm=6.0, line_mm=0.30, seed=seed + 8)
    sh.dirt = np.where(s["grout"] > 0.5, np.clip(0.38 + 0.35 * patch + 0.12 * grain, 0.0, 1.0), np.clip(0.55 * craze + 0.12 * s["shoulder"], 0.0, 1.0))
    sh.chips = chips_mask(sh, g, rng, count=12, r_lo=0.6, r_hi=1.4)
    sh.params = dict(kind="square", tiles_u=g["nu"], tiles_v=g["nv"], pitch_mm=P, joint_mm=J, edge_r_mm=1.0,
                     span_m=[0.6, 0.6], glaze="bright glaze, smoothness 0.93", use="tight curves R 0.3-0.6 (ask 2)")
    return sh


def _profile_glaze(sh, g, rng, seed, s, glazed, sanded=None):
    """Shared surface of a profile trim: glaze waviness where glazed (V mask), sanded grit where sanded, grout at the joints."""
    H, W = sh.H, sh.W
    face = 1.0 - s["grout"]
    peel = noise(H, W, sh.px(2.5), seed + 1) * 0.0045 + noise(H, W, sh.px(12.0), seed + 2) * 0.008
    sand = noise(H, W, sh.px(0.55), seed + 3) * 0.016 + noise(H, W, sh.px(1.5), seed + 4) * 0.006
    grain = noise(H, W, sh.px(0.6), seed + 5)
    sanded = np.zeros_like(glazed) if sanded is None else sanded
    hgt = s["hgt"] + face * (peel * glazed + sand * sanded) + grain * 0.05 * s["grout"]
    return hgt, face, grain


def make_bullnose(name="Bullnose", wd=2048, seed=308):
    """The outside-corner bullnose trim, 150 mm units, mesh UV: V across the unrolled profile, face leg 0-142, round r 8
    142-154.57, top leg 154.57-296.57 mm (the arris's glaze thins on the round, its top a dust ledge)."""
    sh, g, rng = _profile_sheet(name, wd, wd // 4, 1200.0, 300.0, 150.0, 3.0, seed)
    s = tile_surface(sh, g, rng, edge_r=1.8, g_edge=0.7, g_dip=0.5, lip_sd=0.10, tilt_sd_deg=0.0,
                     bow_lo=0.0, bow_hi=0.02, saddle_sd=0.0, joint=3.0)
    v = sh.Y
    rnd = ((v >= 142.0) & (v < 154.57)).astype(np.float32)
    top = (v >= 154.57).astype(np.float32)
    endj = np.maximum(1.0 - smoothstep(0.6, 1.8, v), smoothstep(295.0, 296.4, v))
    hgt, face, grain = _profile_glaze(sh, g, rng, seed, s, glazed=np.ones_like(v))
    gr = np.maximum(s["grout"], endj)
    sh.hgt = hgt + endj * (np.minimum(s["hgt"], -1.0) - s["hgt"])
    n = g["nu"]
    lot = rng.normal(0.0, 0.006, max(n, 1) * g["nv"]).astype(np.float32)
    mottle = noise(sh.H, sh.W, sh.px(25.0), seed + 6) * 0.004
    m_face = (1.0 + lot[g["tid"]]) * (1.0 + mottle) * (1.0 - 0.045 * s["shoulder"])
    sh.tone = np.where(gr > 0.5, (1.0 + 0.10 * grain) * 0.92, m_face)
    sh.thick = np.where(gr > 0.5, 1.0, (0.45 + 0.55 * smoothstep(0.0, 2.2, s["d"])) * (1.0 - 0.22 * rnd))
    sh.grout = gr
    sh.smooth = np.where(gr > 0.5, 0.24 + 0.05 * grain, 0.935 - 0.03 * s["shoulder"] - 0.05 * rnd)
    sh.cavity = np.where(gr > 0.5, 0.78, 1.0 - 0.08 * s["shoulder"])
    patch = np.clip(0.5 + 0.5 * noise(sh.H, sh.W, sh.px(60.0), seed + 7), 0.0, 1.0)
    sh.dirt = np.where(gr > 0.5, np.clip(0.42 + 0.35 * patch, 0.0, 1.0), np.clip(0.12 * s["shoulder"] + 0.35 * top * patch * smoothstep(200.0, 296.0, v), 0.0, 1.0))
    sh.chips = chips_mask(sh, g, rng, count=6, r_lo=0.9, r_hi=2.4, trim=True) * rnd
    sh.params = dict(kind="profile", tiles_u=g["nu"], tiles_v=1, unit_mm=150.0, joint_mm=3.0, span_m=[1.2, 0.3],
                     profile_mm=dict(face=[0, 142], round=[142, 154.57], top=[154.57, 296.57]), round_r_mm=8.0)
    return sh


def make_cove(name="Cove", wd=2048, seed=309):
    """The pool's cove r 50 with 50 mm flats, 150 mm units, mesh UV: floor flat 0-50, round 50-128.5, wall flat 128.5-178.5 mm;
    the floor flat unglazed (the basin floor's mosaic meets it), the round and the wall flat glazed."""
    sh, g, rng = _profile_sheet(name, wd, wd // 8, 1200.0, 180.0, 150.0, 3.0, seed)
    s = tile_surface(sh, g, rng, edge_r=1.8, g_edge=0.7, g_dip=0.5, lip_sd=0.10, tilt_sd_deg=0.0,
                     bow_lo=0.0, bow_hi=0.02, saddle_sd=0.0, joint=3.0)
    v = sh.Y
    floor = (v < 50.0).astype(np.float32)
    rnd = ((v >= 50.0) & (v < 128.5)).astype(np.float32)
    endj = np.maximum(1.0 - smoothstep(0.6, 1.8, v), smoothstep(177.0, 178.4, v))
    hgt, face, grain = _profile_glaze(sh, g, rng, seed, s, glazed=1.0 - floor, sanded=floor)
    gr = np.maximum(s["grout"], endj)
    sh.hgt = hgt + endj * (np.minimum(s["hgt"], -1.0) - s["hgt"])
    n = g["nu"]
    lot = rng.normal(0.0, 0.006, max(n, 1) * g["nv"]).astype(np.float32)
    m_face = (1.0 + lot[g["tid"]]) * (1.0 - 0.045 * s["shoulder"]) * (1.0 - 0.02 * floor)
    sh.tone = np.where(gr > 0.5, (1.0 + 0.10 * grain) * 0.92, m_face)
    sh.thick = np.where(gr > 0.5, 1.0, 0.45 + 0.55 * smoothstep(0.0, 2.2, s["d"]))
    sh.grout = gr
    sh.smooth = np.where(gr > 0.5, 0.24 + 0.05 * grain, 0.935 * (1.0 - floor) + 0.47 * floor - 0.03 * s["shoulder"])
    sh.cavity = np.where(gr > 0.5, 0.78, 1.0 - 0.08 * s["shoulder"] - 0.10 * rnd * (1.0 - smoothstep(60.0, 110.0, v)))
    patch = np.clip(0.5 + 0.5 * noise(sh.H, sh.W, sh.px(40.0), seed + 7), 0.0, 1.0)
    # the cove's foot holds the scale and the silt (the basin floor's edge)
    foot = 1.0 - smoothstep(20.0, 90.0, v)
    sh.dirt = np.where(gr > 0.5, np.clip(0.45 + 0.35 * patch, 0.0, 1.0), np.clip(0.30 * foot * patch + 0.10 * s["shoulder"], 0.0, 1.0))
    sh.chips = chips_mask(sh, g, rng, count=3, r_lo=0.9, r_hi=2.0, trim=True) * rnd
    sh.params = dict(kind="profile", tiles_u=g["nu"], tiles_v=1, unit_mm=150.0, joint_mm=3.0, span_m=[1.2, 0.18],
                     profile_mm=dict(floor=[0, 50], round=[50, 128.5], wall=[128.5, 178.5]), round_r_mm=50.0)
    return sh


def make_gutter(name="Gutter", wd=2048, seed=310):
    """The roll-out gutter, 150 mm units, mesh UV across its unrolled section from the deck: abrasive deck 0-200, the flush
    porcelain finger grate 200-400 (fingers 12 mm, slots 8 mm across V, the dark wet channel under them), the curb's back
    400-450, its top with the bullnose at the water 450-542.6, the pool face 542.6-692.6 mm."""
    sh, g, rng = _profile_sheet(name, wd, wd // 2, 1200.0, 700.0, 150.0, 3.0, seed)
    s = tile_surface(sh, g, rng, edge_r=1.8, g_edge=0.7, g_dip=0.5, lip_sd=0.10, tilt_sd_deg=0.0,
                     bow_lo=0.0, bow_hi=0.02, saddle_sd=0.0, joint=3.0)
    v = sh.Y
    u = sh.X
    deck = (v < 200.0).astype(np.float32)
    grate = ((v >= 200.0) & (v < 400.0)).astype(np.float32)
    curb = ((v >= 400.0) & (v < 542.6)).astype(np.float32)
    nose = ((v >= 525.0) & (v < 542.6)).astype(np.float32)
    # the fingers: 12 mm wide every 20 mm along U, each finger rounded on top, the slot dark (the channel 40 mm down)
    fu = np.mod(u, 20.0)
    slot = ((fu >= 12.0) & (grate > 0.5)).astype(np.float32)
    fing = np.clip(1.0 - np.abs(fu - 6.0) / 6.0, 0.0, 1.0) * grate * (1.0 - slot)
    endj = np.maximum.reduce([1.0 - smoothstep(0.6, 1.8, v), smoothstep(691.0, 692.4, v), grate * (smoothstep(196.0, 199.0, v) * (1.0 - smoothstep(201.0, 204.0, v))),
                              grate * (smoothstep(396.0, 399.0, v) * (1.0 - smoothstep(401.0, 404.0, v)))])
    hgt, face, grain = _profile_glaze(sh, g, rng, seed, s, glazed=1.0 - deck - grate, sanded=deck)
    gr = np.maximum(s["grout"], endj)
    hgt = hgt - 0.8 * slot + 0.35 * np.sqrt(fing)
    sh.hgt = hgt + endj * (np.minimum(s["hgt"], -1.0) - s["hgt"])
    n = g["nu"]
    lot = rng.normal(0.0, 0.006, max(n, 1) * g["nv"]).astype(np.float32)
    m_face = (1.0 + lot[g["tid"]]) * (1.0 - 0.045 * s["shoulder"]) * (1.0 - 0.03 * deck)
    tone = np.where(gr > 0.5, (1.0 + 0.10 * grain) * 0.92, m_face)
    tone = tone * (1.0 - 0.45 * slot) * (1.0 - 0.04 * grate * (1.0 - fing))
    sh.tone = tone
    sh.thick = np.where(gr > 0.5, 1.0, (0.45 + 0.55 * smoothstep(0.0, 2.2, s["d"])) * (1.0 - 0.2 * nose))
    sh.grout = np.maximum(gr, slot)
    sh.smooth = np.where(gr > 0.5, 0.24 + 0.05 * grain, 0.31 * deck + 0.55 * grate + 0.93 * curb + 0.93 * (v >= 542.6) - 0.03 * s["shoulder"])
    sh.cavity = np.where(gr > 0.5, 0.78, 1.0 - 0.08 * s["shoulder"] - 0.55 * slot - 0.12 * grate * (1.0 - fing))
    patch = np.clip(0.5 + 0.5 * noise(sh.H, sh.W, sh.px(50.0), seed + 7), 0.0, 1.0)
    wetline = smoothstep(560.0, 580.0, v) * (1.0 - smoothstep(600.0, 640.0, v))
    sh.dirt = np.where(gr > 0.5, np.clip(0.48 + 0.35 * patch, 0.0, 1.0),
                       np.clip(0.20 * deck * patch + 0.35 * grate * patch + 0.35 * wetline * patch + 0.10 * s["shoulder"], 0.0, 1.0))
    sh.chips = chips_mask(sh, g, rng, count=6, r_lo=0.9, r_hi=2.4, trim=True) * nose
    sh.params = dict(kind="profile", tiles_u=g["nu"], tiles_v=1, unit_mm=150.0, joint_mm=3.0, span_m=[1.2, 0.7],
                     profile_mm=dict(deck=[0, 200], grate=[200, 400], curb_back=[400, 450], curb_top=[450, 542.6], face=[542.6, 692.6]),
                     grate=dict(finger_mm=12.0, slot_mm=8.0, pitch_mm=20.0))
    return sh


def make_weirlip(name="WeirLip", wd=2048, seed=311):
    """The square porcelain weir lip, 150 mm units, mesh UV across its section: the underside 0-35, the drip groove 35-53, the
    underside to the nose 53-62, the nose face 62-122, the top 122-272 mm (the water's wear at the nose, scale under it)."""
    sh, g, rng = _profile_sheet(name, wd, wd // 4, 1200.0, 280.0, 150.0, 3.0, seed)
    s = tile_surface(sh, g, rng, edge_r=1.8, g_edge=0.7, g_dip=0.5, lip_sd=0.10, tilt_sd_deg=0.0,
                     bow_lo=0.0, bow_hi=0.02, saddle_sd=0.0, joint=3.0)
    v = sh.Y
    under = (v < 62.0).astype(np.float32)
    groove = ((v >= 35.0) & (v < 53.0)).astype(np.float32)
    nosef = ((v >= 62.0) & (v < 122.0)).astype(np.float32)
    top = (v >= 122.0).astype(np.float32)
    endj = np.maximum(1.0 - smoothstep(0.6, 1.8, v), smoothstep(270.0, 271.4, v))
    hgt, face, grain = _profile_glaze(sh, g, rng, seed, s, glazed=1.0 - under, sanded=under * 0.4)
    gr = np.maximum(s["grout"], endj)
    sh.hgt = hgt + endj * (np.minimum(s["hgt"], -1.0) - s["hgt"])
    n = g["nu"]
    lot = rng.normal(0.0, 0.006, max(n, 1) * g["nv"]).astype(np.float32)
    m_face = (1.0 + lot[g["tid"]]) * (1.0 - 0.045 * s["shoulder"]) * (1.0 - 0.10 * groove) * (1.0 - 0.04 * under)
    sh.tone = np.where(gr > 0.5, (1.0 + 0.10 * grain) * 0.92, m_face)
    sh.thick = np.where(gr > 0.5, 1.0, (0.45 + 0.55 * smoothstep(0.0, 2.2, s["d"])) * (1.0 - 0.25 * smoothstep(112.0, 126.0, v) * (1.0 - smoothstep(126.0, 140.0, v))))
    sh.grout = gr
    sh.smooth = np.where(gr > 0.5, 0.24 + 0.05 * grain, 0.92 * (1.0 - under) + 0.55 * under - 0.03 * s["shoulder"])
    sh.cavity = np.where(gr > 0.5, 0.78, 1.0 - 0.08 * s["shoulder"] - 0.35 * groove)
    patch = np.clip(0.5 + 0.5 * noise(sh.H, sh.W, sh.px(40.0), seed + 7), 0.0, 1.0)
    scale = under * (1.0 - smoothstep(0.0, 30.0, np.abs(v - 44.0))) + nosef * smoothstep(62.0, 80.0, v) * (1.0 - smoothstep(90.0, 122.0, v))
    sh.dirt = np.where(gr > 0.5, np.clip(0.45 + 0.35 * patch, 0.0, 1.0), np.clip(0.45 * scale * patch + 0.10 * s["shoulder"], 0.0, 1.0))
    sh.chips = chips_mask(sh, g, rng, count=5, r_lo=0.9, r_hi=2.2, trim=True) * (nosef + top * (1.0 - smoothstep(122.0, 135.0, v)))
    sh.params = dict(kind="profile", tiles_u=g["nu"], tiles_v=1, unit_mm=150.0, joint_mm=3.0, span_m=[1.2, 0.28],
                     profile_mm=dict(under=[0, 35], groove=[35, 53], under_nose=[53, 62], nose=[62, 122], top=[122, 272]))
    return sh


SETS = dict(Field=make_field, Mosaic=make_mosaic, Deck=make_deck, Cap=make_cap, Coping=make_coping, Nosing=make_nosing,
            Field50=make_field50, Bullnose=make_bullnose, Cove=make_cove, Gutter=make_gutter, WeirLip=make_weirlip)


# ------------------------------------------------------------------------------------------------ the real look's face map
# 70_look_build.md section 13 (the tile's real look, Red 2026-11-04: "...我需要你参考Pools游戏 优化瓷砖，以及光打在瓷砖上的表现",
# and "...重点在于法线贴图让瓷砖表现出一种不规则的感觉，中间凹四角突"). In the real look FrontRooms/Surface draws each tile's shape
# itself (the dish, its corners, twist, tilt, a bowed edge, the cushion edge, the struck grout, lippage: analytic, from a hash
# of the tile's cell, so no 8-bit steps and no repeat), and reads only the glaze's waviness from this map, through a window
# of the map that each tile takes at random (so the waviness never repeats either and stops at the tile's edge, as each tile
# is its own glaze). No tile structure in the map: R, G the glaze's slope (dh/du, dh/dv, radians) as 0.5 + slope * FACE_K
# (+-2 degrees over the byte: a step is 0.016 degrees, 28 x finer than a normal map's 0.45), B, A the grout's grain slope as
# 0.5 + slope * GRAIN_K (+-31 degrees), linear, wrapping, FACE_SPAN_MM square (the shader's FR_TILE_FACE_SPAN).
# The waviness is GZ-C's (research/level_space/73_pool_glaze.md section 3.1, Red's pick of 16:4x): orange peel on 5.0 mm at
# 3.0 um and a swell on 14 mm at 6 um (about 0.2 degrees rms per axis), band noise as noise().
FACE_SPAN_MM = 600.0
FACE_K = 0.5 / 0.035
GRAIN_K = 0.5 / 0.6
FACE_PEEL = (5.0, 0.0030)      # wavelength mm, std mm
FACE_SWELL = (14.0, 0.0060)
FACE_GRAIN = ((1.6, 0.030), (0.8, 0.012))


def _slopes(h, mm):
    du = (np.roll(h, -1, axis=1) - np.roll(h, 1, axis=1)) / (2.0 * mm)
    dv = (np.roll(h, -1, axis=0) - np.roll(h, 1, axis=0)) / (2.0 * mm)
    return du.astype(np.float32), dv.astype(np.float32)


def make_face(out_tex, preview_dir, manifest, name="Face", wd=1024, seed=1500):
    """The real look's face map (see above): desktop wd x wd, phone half; returns the GUIDs {"F": .., "F_Phone": ..}."""
    sh = Sheet(name, wd, wd, FACE_SPAN_MM, FACE_SPAN_MM)
    H, W = sh.H, sh.W
    h = noise(H, W, sh.px(FACE_PEEL[0]), seed + 1) * FACE_PEEL[1] + noise(H, W, sh.px(FACE_SWELL[0]), seed + 2) * FACE_SWELL[1]
    g = sum(noise(H, W, sh.px(lam), seed + 3 + i) * a for i, (lam, a) in enumerate(FACE_GRAIN))
    hu, hv = _slopes(h, sh.mm_u)
    gu, gv = _slopes(g, sh.mm_u)
    rms_deg = float(np.degrees(np.sqrt(0.5 * (hu.var() + hv.var()))))
    var_true = float(hu.var() + hv.var())
    out = {}
    stats = dict(rms_deg_per_axis=round(rms_deg, 4), var_rad2_true=var_true, span_mm=FACE_SPAN_MM, face_k=FACE_K, grain_k=GRAIN_K,
                 peel=list(FACE_PEEL), swell=list(FACE_SWELL), grain=[list(x) for x in FACE_GRAIN])
    for tier, k in (("", SS), ("_Phone", SS * 2)):
        su, sv, tu, tv = down(hu, k), down(hv, k), down(gu, k), down(gv, k)
        img = np.stack([0.5 + su * FACE_K, 0.5 + sv * FACE_K, 0.5 + tu * GRAIN_K, 0.5 + tv * GRAIN_K], axis=-1)
        clip = float(np.mean((img < 0.0) | (img > 1.0)))
        # the slope variance each mip of this map still holds (box mips, as the importer's), for the shader's roughness
        lost = []
        a_u, a_v = su.copy(), sv.copy()
        while True:
            lost.append(round(var_true - float(a_u.var() + a_v.var()), 9))
            if a_u.shape[0] < 2:
                break
            a_u, a_v = down(a_u, 2), down(a_v, 2)
        stats["lost_var_by_mip" + tier] = lost
        stats["clipped_share" + tier] = clip
        fn = "PoolTile_%s_F%s.png" % (name, tier)
        path = os.path.join(out_tex, fn)
        Image.fromarray(np.ascontiguousarray(to8(np.clip(img, 0.0, 1.0))[::-1]), "RGBA").save(path, optimize=False, compress_level=9)
        guid = write_face_meta(path, img.shape[0])
        manifest["textures"][fn] = dict(size=[img.shape[1], img.shape[0]], sha1=sha1(path), guid=guid, bytes=os.path.getsize(path),
                                        linear=True, normal=False, alpha=True, wrapV="repeat", face=True)
        out["F" + tier] = guid
        if preview_dir and tier == "":
            os.makedirs(preview_dir, exist_ok=True)
            vis = np.clip(0.5 + (img[..., :2] - 0.5) * 6.0, 0, 1)
            Image.fromarray(to8(np.concatenate([vis, np.full(vis.shape[:2] + (1,), 0.5, np.float32)], -1))[::-1], "RGB").save(
                os.path.join(preview_dir, "preview_%s_slopes.png" % name))
    manifest["sets"][name] = stats
    print("face %s: glaze slope rms %.3f deg per axis, true variance %.3g rad2; lost by mip (desktop) %s" % (
        name, rms_deg, var_true, stats["lost_var_by_mip"][:7]), flush=True)
    return out


def write_face_meta(png, size):
    """The face map is data (linear, not a normal map: the shader decodes it). Standalone BC7, iOS ASTC 4x4 (both keep the
    smooth slopes); WebGL never samples it (the real look is compiled out there), so its WebGL copy is 32 px."""
    meta = png + ".meta"
    guid = existing_guid(meta)
    fmt = dict(DefaultTexturePlatform=(-1, 2, 0, size), Standalone=(25, 1, 1, size), Android=(-1, 1, 0, size),
               VisionOS=(-1, 1, 0, size), WebGL=(-1, 1, 1, 32), iOS=(48, 1, 1, size))
    plats = ""
    for t in ("DefaultTexturePlatform", "Standalone", "Android", "VisionOS", "WebGL", "iOS"):
        f, comp, over, sz = fmt[t]
        plats += PLATFORM_OVR.format(target=t, size=(4096 if t == "DefaultTexturePlatform" else max(sz, 32)), fmt=f, comp=comp, over=over)
    write_text(meta, META_TEX.format(guid=guid, size=max(size, 32), wrapv=0, alpha=1, ttype=0, platforms=plats))
    return guid


# ------------------------------------------------------------------------------------------------ output
META_TEX = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 1
    sRGBTexture: 0
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: {size}
  textureSettings:
    serializedVersion: 2
    filterMode: 2
    aniso: 16
    mipBias: 0
    wrapU: 0
    wrapV: {wrapv}
    wrapW: 0
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 2
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: {alpha}
  alphaIsTransparency: 0
  spriteTessellationMethod: 0
  spriteTessellationDetail: -1
  spriteGeometrySubdivision: -1
  textureType: {ttype}
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
{platforms}  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData:
    physicsShape: []
    bones: []
    spriteID:
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName:
  pSDRemoveMatte: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"""

PLATFORM = """  - serializedVersion: 4
    buildTarget: {target}
    maxTextureSize: {size}
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: {comp}
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
"""

PLATFORM_OVR = """  - serializedVersion: 4
    buildTarget: {target}
    maxTextureSize: {size}
    resizeAlgorithm: 0
    textureFormat: {fmt}
    textureCompression: {comp}
    compressionQuality: 100
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: {over}
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 1
"""


def existing_guid(meta_path):
    if os.path.exists(meta_path):
        with open(meta_path, "r") as f:
            m = re.search(r"^guid: ([0-9a-f]{32})", f.read(), re.M)
            if m:
                return m.group(1)
    return uuid.uuid4().hex


def write_text(path, text):
    with open(path, "w", newline="\n") as f:
        f.write(text)


def sha1(path):
    h = hashlib.sha1()
    with open(path, "rb") as f:
        h.update(f.read())
    return h.hexdigest()


def write_tex_meta(png, size, normal, alpha, clamp_v):
    meta = png + ".meta"
    guid = existing_guid(meta)
    plats = "".join(PLATFORM.format(target=t, size=(4096 if t == "DefaultTexturePlatform" else max(size, 32)),
                                    comp=(2 if t == "DefaultTexturePlatform" else 1))
                    for t in ("DefaultTexturePlatform", "Standalone", "Android", "VisionOS", "WebGL", "iOS"))
    write_text(meta, META_TEX.format(guid=guid, size=max(size, 32), wrapv=1 if clamp_v else 0, alpha=1 if alpha else 0,
                                     ttype=1 if normal else 0, platforms=plats))
    return guid


def to8(a):
    return np.clip(np.round(a * 255.0), 0, 255).astype(np.uint8)


def finish(sh, out_tex, preview_dir, manifest):
    """Downsample to the desktop and phone maps, quantise with a little dither, write PNG + .meta."""
    rng = np.random.default_rng(int(hashlib.md5(sh.name.encode("ascii")).hexdigest()[:8], 16))
    tone_code = np.clip((sh.tone - 0.75) / 0.5, 0.0, 1.0)
    data = np.stack([tone_code, np.clip(sh.thick, 0, 1), np.clip(sh.grout, 0, 1)], axis=-1)
    mask = np.stack([np.clip(sh.smooth, 0, 1), np.clip(sh.cavity, 0, 1), np.clip(sh.dirt, 0, 1), np.clip(sh.chips, 0, 1)], axis=-1)
    n_hi = normals(sh.hgt, sh.mm_u, sh.mm_v, wrap_v=sh.wrap_v)
    outs = {}
    for tier, k in (("", SS), ("_Phone", SS * 2)):
        d = down(data, k)
        m = down(mask, k)
        n = down(n_hi, k)
        n /= np.linalg.norm(n, axis=-1, keepdims=True)
        dither = (rng.random(n.shape[:2] + (1,)) + rng.random(n.shape[:2] + (1,)) - 1.0) / 255.0
        nenc = np.clip((n * 0.5 + 0.5) + dither * 0.5, 0, 1)
        flip = lambda a: np.ascontiguousarray(a[::-1])
        files = [("D", Image.fromarray(flip(to8(d)), "RGB"), False, False),
                 ("N", Image.fromarray(flip(to8(nenc)), "RGB"), True, False),
                 ("S", Image.fromarray(flip(to8(m)), "RGBA"), False, True)]
        for key, im, is_n, has_a in files:
            fn = "PoolTile_%s_%s%s.png" % (sh.name, key, tier)
            path = os.path.join(out_tex, fn)
            im.save(path, optimize=False, compress_level=9)
            guid = write_tex_meta(path, max(im.size), is_n, has_a, clamp_v=not sh.wrap_v)
            manifest["textures"][fn] = dict(size=list(im.size), sha1=sha1(path), guid=guid, bytes=os.path.getsize(path),
                                            linear=True, normal=is_n, alpha=has_a, wrapV="clamp" if not sh.wrap_v else "repeat")
            outs[key + tier] = guid
        if preview_dir and tier == "":
            preview(sh.name, d, m, n, preview_dir)
    manifest["sets"][sh.name] = sh.params
    return outs


def preview(name, d, m, n, out):
    """QA: the maps decoded with porcelain white glaze and grey-green grout under one raking lamp (not the game's light)."""
    os.makedirs(out, exist_ok=True)
    base = np.array([0.855, 0.888, 0.905], np.float32)
    edge = np.array([0.66, 0.68, 0.66], np.float32)
    grout = np.array([0.40, 0.45, 0.41], np.float32)
    tone = (0.75 + 0.5 * d[..., 0])[..., None]
    alb = (edge + (base - edge) * d[..., 1:2]) * tone
    alb = alb * (1 - d[..., 2:3]) + grout * tone * d[..., 2:3]
    L = np.array([0.25, 0.55, 0.80], np.float32); L /= np.linalg.norm(L)
    V = np.array([0.0, -0.35, 0.94], np.float32); V /= np.linalg.norm(V)
    Hh = (L + V) / np.linalg.norm(L + V)
    ndl = np.clip((n * L).sum(-1), 0, 1)[..., None]
    ndh = np.clip((n * Hh).sum(-1), 0, 1)
    rough = 1.0 - m[..., 0]
    a2 = np.maximum(rough ** 4, 1e-4)
    spec = (a2 / (np.pi * ((ndh * ndh) * (a2 - 1) + 1) ** 2))[..., None] * 0.04 * ndl
    col = alb * (0.30 + 0.70 * ndl) * m[..., 1:2] + spec * 0.25
    img = np.clip(col, 0, 1) ** (1 / 2.2)
    h, w = img.shape[:2]
    ch = min(h, 512)
    crop = img[::-1][:ch, :min(w, 1024)]
    Image.fromarray(to8(crop), "RGB").save(os.path.join(out, "preview_%s_1to1.png" % name))
    small = img[::-1][::max(1, h // 512), ::max(1, w // 512)]
    Image.fromarray(to8(small), "RGB").save(os.path.join(out, "preview_%s_whole.png" % name))


# ------------------------------------------------------------------------------------------------ materials
MAT = """%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!21 &2100000
Material:
  serializedVersion: 8
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: {name}
  m_Shader: {{fileID: 4800000, guid: {shader}, type: 3}}
  m_Parent: {{fileID: 0}}
  m_ModifiedSerializedProperties: 0
  m_ValidKeywords:
{keywords}  m_InvalidKeywords: []
  m_LightmapFlags: 4
  m_EnableInstancingVariants: 0
  m_DoubleSidedGI: 0
  m_CustomRenderQueue: -1
  stringTagMap: {{}}
  disabledShaderPasses: []
  m_LockedProperties:
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs:
    - _BaseMap:
        m_Texture: {{fileID: 2800000, guid: {d}, type: 3}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _BumpMap:
        m_Texture: {{fileID: 2800000, guid: {n}, type: 3}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _EmissionMap:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _MacroMap:
        m_Texture: {{fileID: 2800000, guid: {macro}, type: 3}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _MaskMap:
        m_Texture: {{fileID: 2800000, guid: {s}, type: 3}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _TileFaceMap:
        m_Texture: {{fileID: 2800000, guid: {f}, type: 3}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    m_Ints: []
    m_Floats:
{floats}    m_Colors:
{colors}  m_BuildTextureStacks: []
  m_AllowLocking: 1
"""

META_MAT = """fileFormatVersion: 2
guid: {guid}
NativeFormatImporter:
  externalObjects: {{}}
  mainObjectFileID: 2100000
  userData:
  assetBundleName:
  assetBundleVariant:
"""

# Colours are given as the Inspector shows them (gamma); Unity linearises Color properties for the shader.
WHITE = (0.93, 0.95, 0.96)        # Red 10-31 17:19, porcelain white as tried in the level-space clones
WHITE_EDGE = (0.80, 0.81, 0.80)   # the white body where the glaze thins on the cushion edge
AQUA = (0.40, 0.76, 0.77)         # 16 section 5: the waterline course
AQUA_EDGE = (0.70, 0.86, 0.86)    # a coloured glaze breaks lighter at the edge
GROUT = (0.66, 0.70, 0.67)        # light grey-green (16 section 0 item 2, PR9); REC, options in the sheet
DIRT = (0.36, 0.33, 0.27)
NAVY = (0.24, 0.27, 0.31)         # the stair nosing (BH 193: deep blue-grey, 50 mm on the tread, 25 mm down the riser)
NAVY_EDGE = (0.42, 0.45, 0.50)
LANE = (0.10, 0.12, 0.16)         # lane lines: near-black blue (period black lane markings)
LANE_EDGE = (0.10, 0.12, 0.16)
MOSAIC_WHITE = (0.92, 0.94, 0.95)
DECK_WHITE = (0.86, 0.88, 0.88)   # unglazed abrasive porcelain: a shade below the glaze, cool

# name: set, mesh_uv, base, edge, wear, tile size (m), grid, macro tone/dirt, wet band, waterline (scale line), shade
MATERIALS = [
    ("Field", "Field", False, WHITE, WHITE_EDGE, 0.0, (1.2, 1.2), (8, 8), (0.0, 0.0), (0.02, 0.04, 1.0, 0.75), (0.0, 0.010, 0.002, 0.0), 0.6),
    ("FieldClub", "Field", False, WHITE, WHITE_EDGE, 1.0, (1.2, 1.2), (8, 8), (0.10, 0.08), (0.05, 0.08, 1.0, 0.75), (0.6, 0.012, 0.004, 0.0), 0.6),
    ("Course", "Field", False, AQUA, AQUA_EDGE, 0.0, (1.2, 1.2), (8, 8), (0.0, 0.0), (0.02, 0.04, 1.0, 0.75), (0.0, 0.010, 0.002, 0.0), 0.6),
    ("CourseClub", "Field", False, AQUA, AQUA_EDGE, 1.0, (1.2, 1.2), (8, 8), (0.08, 0.08), (0.05, 0.08, 1.0, 0.75), (0.6, 0.012, 0.004, 0.0), 0.6),
    ("Cap", "Cap", False, WHITE, WHITE_EDGE, 1.0, (1.2, 0.15), (8, 1), (0.10, 0.08), (0.05, 0.08, 1.0, 0.75), (0.0, 0.012, 0.004, 0.0), 0.6),
    ("Mosaic", "Mosaic", False, MOSAIC_WHITE, MOSAIC_WHITE, 0.0, (0.6, 0.6), (24, 24), (0.0, 0.0), (0.02, 0.04, 1.0, 0.0), (0.0, 0.010, 0.002, 0.0), 0.3),
    ("MosaicClub", "Mosaic", False, MOSAIC_WHITE, MOSAIC_WHITE, 0.6, (0.6, 0.6), (24, 24), (0.06, 0.05), (0.05, 0.08, 1.0, 0.0), (0.4, 0.012, 0.004, 0.0), 0.3),
    ("Lane", "Mosaic", False, LANE, LANE_EDGE, 0.4, (0.6, 0.6), (24, 24), (0.0, 0.0), (0.05, 0.08, 1.0, 0.0), (0.0, 0.012, 0.004, 0.0), 0.3),
    ("Deck", "Deck", False, DECK_WHITE, DECK_WHITE, 1.0, (1.2, 1.2), (8, 8), (0.12, 0.12), (0.05, 0.08, 1.0, 0.0), (0.0, 0.012, 0.004, 0.0), 0.0),
    ("Tread", "Deck", True, DECK_WHITE, DECK_WHITE, 0.0, (1.2, 1.2), (8, 8), (0.0, 0.0), (0.02, 0.04, 1.0, 0.75), (0.0, 0.010, 0.002, 0.0), 0.0),
    ("Riser", "Field", True, WHITE, WHITE_EDGE, 0.0, (1.2, 1.2), (8, 8), (0.0, 0.0), (0.02, 0.04, 1.0, 0.75), (0.0, 0.010, 0.002, 0.0), 0.6),
    ("Nosing", "Nosing", True, NAVY, NAVY_EDGE, 0.0, (1.2, 0.075), (8, 1), (0.0, 0.0), (0.02, 0.04, 1.0, 0.75), (0.0, 0.010, 0.002, 0.0), 0.0),
    ("Coping", "Coping", True, WHITE, WHITE_EDGE, 1.0, (1.2, 0.34), (4, 1), (0.08, 0.08), (0.05, 0.08, 1.0, 0.75), (0.6, 0.012, 0.004, 0.0), 0.0),
    # mesh-UV twins of the field and the course (UVs in metres, joints running on across corners and arches): the structures
    # scene's tile and band (FrontRoomsPoolMaterials.Tile / Band; contract W/apply/pool_tile_real_contract)
    ("FieldMesh", "Field", True, WHITE, WHITE_EDGE, 0.0, (1.2, 1.2), (8, 8), (0.0, 0.0), (0.02, 0.04, 1.0, 0.75), (0.0, 0.010, 0.002, 0.0), 0.6),
    ("CourseMesh", "Field", True, AQUA, AQUA_EDGE, 0.0, (1.2, 1.2), (8, 8), (0.0, 0.0), (0.02, 0.04, 1.0, 0.75), (0.0, 0.010, 0.002, 0.0), 0.6),
    # structures assets (scene 4, P2): tiled curves by radius (ask 2) and the four trim pieces, all on mesh UVs in metres
    ("Field108Mesh", "Field", True, WHITE, WHITE_EDGE, 0.0, (0.872727, 0.872727), (8, 8), (0.0, 0.0), (0.02, 0.04, 1.0, 0.75), (0.0, 0.010, 0.002, 0.0), 0.6),
    ("Field75Mesh", "Field", True, WHITE, WHITE_EDGE, 0.0, (0.6, 0.6), (8, 8), (0.0, 0.0), (0.02, 0.04, 1.0, 0.75), (0.0, 0.010, 0.002, 0.0), 0.6),
    ("Field50Mesh", "Field50", True, WHITE, WHITE_EDGE, 0.0, (0.6, 0.6), (12, 12), (0.0, 0.0), (0.02, 0.04, 1.0, 0.75), (0.0, 0.010, 0.002, 0.0), 0.6),
    ("MosaicMesh", "Mosaic", True, MOSAIC_WHITE, MOSAIC_WHITE, 0.0, (0.6, 0.6), (24, 24), (0.0, 0.0), (0.02, 0.04, 1.0, 0.0), (0.0, 0.010, 0.002, 0.0), 0.3),
    ("Bullnose", "Bullnose", True, WHITE, WHITE_EDGE, 0.0, (1.2, 0.3), (8, 1), (0.0, 0.0), (0.02, 0.04, 1.0, 0.75), (0.0, 0.010, 0.002, 0.0), 0.0),
    ("Cove", "Cove", True, WHITE, WHITE_EDGE, 0.0, (1.2, 0.18), (8, 1), (0.0, 0.0), (0.02, 0.04, 1.0, 0.75), (0.0, 0.010, 0.002, 0.0), 0.0),
    ("Gutter", "Gutter", True, WHITE, WHITE_EDGE, 0.0, (1.2, 0.7), (8, 1), (0.0, 0.0), (0.02, 0.04, 1.0, 0.75), (0.0, 0.010, 0.002, 0.0), 0.0),
    ("WeirLip", "WeirLip", True, WHITE, WHITE_EDGE, 0.0, (1.2, 0.28), (8, 1), (0.0, 0.0), (0.02, 0.04, 1.0, 0.75), (0.0, 0.010, 0.002, 0.0), 0.0),
]

# The real look's per-kind values (70_look_build.md section 13; read only while FrontRoomsPoolTile.Look is not Today):
#   _TileForm  (shape scale: the dish, its corners, twist, tilt, a bowed edge; joint jitter mm: each tile's offset in its joint;
#               two-lobe glaze 1 / one lobe 0 (unglazed); gloss variety: each tile's glaze gloss +-)
#   _TileForm2 (peel variety: each tile's waviness strength +-; the face's smoothness in the mask (the glaze lobes' 1.0);
#               lippage sd mm against the neighbours, on the cushion; face source 1 = the analytic cushion and grout with the
#               face map's waviness, 0 = the set's own normal map, its trims and ribs kept)
#   _TileEdge  (joint mm, cushion radius mm, face corner radius mm, grout depth at the cushion's foot mm): the set's geometry as
#               the maps draw it (make_field etc.), so the analytic cushion meets the maps' tone and gloss bands
FIELD_REAL = ((1.0, 0.5, 1.0, 0.03), (0.4, 0.935, 0.12, 1.0), (3.0, 1.8, 1.2, 0.7))
MOSAIC_REAL = ((0.25, 0.3, 0.0, 0.04), (0.0, 0.47, 0.05, 0.0), (2.0, 1.0, 1.0, 0.6))
DECK_REAL = ((0.5, 0.6, 0.0, 0.03), (0.0, 0.31, 0.15, 0.0), (4.0, 1.4, 1.5, 1.0))
REAL = dict(
    Field=FIELD_REAL, FieldClub=FIELD_REAL, Course=FIELD_REAL, CourseClub=FIELD_REAL, Riser=FIELD_REAL, FieldMesh=FIELD_REAL,
    CourseMesh=FIELD_REAL,
    Cap=((0.6, 0.5, 1.0, 0.03), (0.0, 0.935, 0.08, 0.0), (3.0, 1.8, 1.2, 0.7)),
    Mosaic=MOSAIC_REAL, MosaicClub=MOSAIC_REAL, Lane=MOSAIC_REAL,
    Deck=DECK_REAL, Tread=DECK_REAL,
    # profiles (mesh UV across the unrolled profile): no shape, no jitter (they would bend the ribs and the nose), glazed
    Nosing=((0.0, 0.0, 1.0, 0.02), (0.0, 0.90, 0.0, 0.0), (3.0, 1.6, 0.0, 0.7)),
    Coping=((0.0, 0.0, 1.0, 0.02), (0.0, 0.92, 0.0, 0.0), (3.0, 2.0, 0.0, 0.8)),
    # structures assets (P2): the field at 109.1 and 75 mm (its geometry scaled with it: joint, cushion, corner, grout depth),
    # the 2 in set's own, the mosaic's; the trims as the profiles above (no shape, glazed)
    Field108Mesh=((0.9, 0.36, 1.0, 0.03), (0.4, 0.935, 0.09, 1.0), (2.182, 1.309, 0.873, 0.509)),
    Field75Mesh=((0.8, 0.25, 1.0, 0.03), (0.4, 0.935, 0.06, 1.0), (1.5, 0.9, 0.6, 0.35)),
    Field50Mesh=((0.7, 0.2, 1.0, 0.03), (0.4, 0.93, 0.05, 1.0), (1.6, 1.0, 0.8, 0.45)),
    MosaicMesh=MOSAIC_REAL,
    Bullnose=((0.0, 0.0, 1.0, 0.02), (0.0, 0.935, 0.0, 0.0), (3.0, 1.8, 0.0, 0.7)),
    Cove=((0.0, 0.0, 1.0, 0.02), (0.0, 0.935, 0.0, 0.0), (3.0, 1.8, 0.0, 0.7)),
    Gutter=((0.0, 0.0, 1.0, 0.02), (0.0, 0.93, 0.0, 0.0), (3.0, 1.8, 0.0, 0.7)),
    WeirLip=((0.0, 0.0, 1.0, 0.02), (0.0, 0.92, 0.0, 0.0), (3.0, 1.8, 0.0, 0.7)),
)


def write_materials(out_dir, guids, face, manifest):
    for (name, setname, mesh_uv, base, edge, wear, size, grid, macro, wet, wline, shade) in MATERIALS:
        for tier in ("", "_Phone"):
            mname = "Prop_PoolTile_%s%s" % (name, tier)
            kw = "  - _FR_TILE\n" + ("  - _FR_MESH_UV\n" if mesh_uv else "")
            floats = [("_BumpScale", 1), ("_CeilingGrime", 0), ("_CeilingHeight", 2.9), ("_Cull", 2), ("_Cutoff", 0.5),
                      ("_FloorGrime", 0.25 if wear > 0.5 and not mesh_uv else 0), ("_FloorGrimeHeight", 0.30),
                      ("_MacroDirt", macro[1]), ("_MacroTone", macro[0]), ("_MeshUV", 1 if mesh_uv else 0), ("_Metallic", 0),
                      ("_OcclusionStrength", 1), ("_Smoothness", 1), ("_StainStrength", 0), ("_UseEmission", 0),
                      ("_WetStrength", 0), ("_UseTile", 1), ("_TileWear", wear), ("_TileShade", shade), ("_WaterY", -1000)]
            colors = [("_BaseColor", base + (1,)), ("_EmissionColor", (0, 0, 0, 1)), ("_StainColor", (0.42, 0.31, 0.16, 1)),
                      ("_TileSize", (size[0], size[1], 0, 0)), ("_TileGrid", (grid[0], grid[1], 0.18, 0.010 if wear > 0.5 else 0.004)),
                      ("_GroutColor", GROUT + (1,)), ("_EdgeColor", edge + (1,)), ("_DirtColor", DIRT + (1,)),
                      ("_TileLot", (0.025 if wear > 0.5 else 0.0, 0.035, 0, 0)), ("_WetBand", wet), ("_WaterLine", wline),
                      ("_TileForm", REAL[name][0]), ("_TileForm2", REAL[name][1]), ("_TileEdge", REAL[name][2])]
            fl = "".join("    - %s: %s\n" % (k, ("%g" % v)) for k, v in floats)
            co = "".join("    - %s: {r: %g, g: %g, b: %g, a: %g}\n" % ((k,) + tuple(v)) for k, v in colors)
            g = guids[setname]
            path = os.path.join(out_dir, mname + ".mat")
            write_text(path, MAT.format(name=mname, shader=SHADER_GUID, keywords=kw, d=g["D" + tier], n=g["N" + tier],
                                        s=g["S" + tier], f=face["F" + tier], macro=MACRO_GUID, floats=fl, colors=co))
            mg = existing_guid(path + ".meta")
            write_text(path + ".meta", META_MAT.format(guid=mg))
            manifest["materials"][mname] = dict(guid=mg, set=setname, tier=tier or "desktop", mesh_uv=mesh_uv, wear=wear,
                                                base=base, edge=edge, tile_size=size, grid=grid, real=REAL[name], sha1=sha1(path))


def folder_meta(path):
    meta = path.rstrip("/") + ".meta"
    if not os.path.exists(meta):
        write_text(meta, "fileFormatVersion: 2\nguid: %s\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n"
                         "  userData: \n  assetBundleName: \n  assetBundleVariant: \n" % uuid.uuid4().hex)


def main():
    ap = argparse.ArgumentParser()
    here = os.path.dirname(os.path.abspath(__file__))
    root = os.path.abspath(os.path.join(here, "..", "..", ".."))
    ap.add_argument("--out", default=os.path.join(root, "Assets", "Resources", "Surfaces", "Pool"))
    ap.add_argument("--sets", default=",".join(list(SETS.keys()) + ["Face"]))
    ap.add_argument("--preview", default="")
    ap.add_argument("--no-materials", action="store_true")
    a = ap.parse_args()
    out_tex = os.path.join(a.out, "Textures")
    os.makedirs(out_tex, exist_ok=True)
    folder_meta(a.out)
    folder_meta(out_tex)
    man_path = os.path.join(out_tex, "pool_tile_manifest.json")
    manifest = dict(textures={}, sets={}, materials={})
    if os.path.exists(man_path):
        with open(man_path) as f:
            old = json.load(f)
        for k in manifest:
            manifest[k].update(old.get(k, {}))
    guids = {}
    face = None
    for name in a.sets.split(","):
        if name == "Face":
            face = make_face(out_tex, a.preview, manifest)
            continue
        sh = SETS[name]()
        guids[name] = finish(sh, out_tex, a.preview, manifest)
        print("set %-7s %dx%d desktop, %dx%d phone" % (name, sh.wd, sh.hd, sh.wd // 2, sh.hd // 2), flush=True)
        del sh
    # sets not regenerated this run keep their GUIDs from the manifest
    for name in SETS:
        if name not in guids:
            gs = {}
            for key in ("D", "N", "S"):
                for tier in ("", "_Phone"):
                    e = manifest["textures"].get("PoolTile_%s_%s%s.png" % (name, key, tier))
                    if e:
                        gs[key + tier] = e["guid"]
            if len(gs) == 6:
                guids[name] = gs
    if face is None:
        # the face map not regenerated this run keeps its GUIDs from the manifest
        face = {}
        for tier in ("", "_Phone"):
            e = manifest["textures"].get("PoolTile_Face_F%s.png" % tier)
            if e:
                face["F" + tier] = e["guid"]
    if not a.no_materials:
        missing = [m[1] for m in MATERIALS if m[1] not in guids]
        if missing:
            raise SystemExit("cannot write materials, sets missing: %s" % sorted(set(missing)))
        if len(face) != 2:
            raise SystemExit("cannot write materials, the face map is missing (run with --sets Face)")
        write_materials(a.out, guids, face, manifest)
    write_text(man_path, json.dumps(manifest, indent=1, sort_keys=True) + "\n")
    folder_meta(os.path.join(a.out))
    print("done: %d textures, %d materials -> %s" % (len(manifest["textures"]), len(manifest["materials"]), a.out))


if __name__ == "__main__":
    main()
