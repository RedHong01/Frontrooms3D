#!/usr/bin/env python3
# FrontRooms structures trim sheet, woven wire, sprayed fireproofing and the access floor laminate. Game visual line,
# structures_assets step 2 group G1 (research/structures_assets/30_plan.md packages P3 structures_trim_sheet and P8
# structures_surfaces; 20_research.md Part B B.6 item 4, F2, F8 (d), F14; Part C C.6 item 16). ASCII only.
# Python 3 + numpy + Pillow (W/venv). Deterministic: fixed seeds, the same files every run.
"""
Writes into Assets/Resources/Surfaces (the game's surfaces folder; Resources/Surfaces/Textures takes the import rules of
FrontRoomsSurfaceTextureImporter by suffix: _A sRGB albedo, _N normal map, _S linear mask, BC7 HQ, trilinear, aniso 16):

  Prop_StructTrim (+ _Phone)   FrontRooms/Surface, _FR_MESH_UV + _FR_SHEEN (_Metallic 1 x mask B per texel, _Sheen 0), _TileSize 1:
      Textures/StructTrim_A/_N/_S.png 2048 x 1024 (1 m of u across, 0.49 mm a texel along a strip); StructTrimPhone_* 1024 x 512.
      The phone twin turns metal off (_Metallic 0: no realtime probes at Medium) and its albedo carries each metal texel lit
      (F0 x PHONE_LIT_METAL, linear; G1 review FIX 3).
      16 strips of 64 px, top to bottom, the same table as FrontRoomsStructureProfiles.Strip (P1):
        0 Sealant            grey polyurethane, tooled concave, adhesion lines at both edges
        1 ShoeAluminium      clear anodised aluminium (glass shoe, cap rail), brushed along u, dust low
        2 PipePaint          grey enamel on steel, chipped to the steel here and there (metal per texel)
        3 AbrasiveNosing     cast aluminium nosing, three bands of dark abrasive grit on its top (P1 AbrasiveNosing's V)
        4 WallAngle          white baked enamel steel (ceiling wall angle)
        5 PourStop           galvanised bent plate, spangle, concrete splash on its lower half
        6 FasciaBand         Office fascia: off-white latex on board, roller orange peel
        7 CarpetBar          clear anodised aluminium carpet nosing: ribbed top leg, smooth worn nose, plain front leg (P1 CarpetBar's V)
        8 FasciaCap          dark bronze anodised aluminium cap (period storefront finish), brushed
        9 SteelAngleNosing   black enamel steel angle, the arris worn to steel
       10 BullnoseBlockEnd   painted bullnose concrete block, open pores under the paint, the round's shading
       11 CurbAngle          the dock's steel curb angle: safety yellow worn off the top, rust bloom, bare steel
       12 Sawcut             broom-finished concrete with a 4 mm sawcut down the middle, sealant and dirt in it
       13 Galvanised         hot-dip galvanised steel, spangle, dull
       14 StainlessBrushed   No. 4 brushed stainless (pool glass shoes and caps)
       15 TBarWhite          the ceiling grid's white tee face, a crisp painted edge
  Prop_WireMesh  URP Lit, alpha clipped, both faces (woven wire partition mesh, 10 ga wire on 1 1/2 in diamonds, crimped at the
      crossings, galvanised): Textures/WireMesh_A.png 512 x 512 RGBA (alpha = wire, mips preserve coverage), WireMesh_N.png;
      the map holds 4 x 4 diamonds = 0.1524 m (the material's tiling 6.5617 a metre of mesh UV).
  Prop_Fireproofing (+ _Phone)  FrontRooms/Surface, _FR_MESH_UV, _TileSize 1 (sprayed cementitious fireproofing on steel: lumpy,
      fibrous, off-white grey): Textures/Fireproofing_A/_N/_S.png 1024 x 1024 (1 m), FireproofingPhone_* 512.
  Prop_AccessFloor (+ _Phone)  FrontRooms/Surface, _FR_MESH_UV, _TileSize 1.2 (P8: a 1988 computer room's access floor panel,
      high-pressure laminate in a light warm grey with a fine chip pattern on 600 mm panels, a black PVC edge at each joint, each
      panel its own lot and a quarter turn of the chip pattern): Textures/AccessFloor_A/_N/_S.png 1024 x 1024 = 2 x 2 panels.
  Prop_RailEnamel  FrontRooms/Surface, _FR_MESH_UV: Painted_Metal.mat as it is in the project with _TileSize 0.3, _Smoothness 1.2,
      _BumpScale 0.8 (painted handrail steel at the hand's scale; no new map; G1 review FIX 4).

Usage (from the project root): python Tools/lookdev/structures_trim/gen_struct_trim.py [--preview <dir>]
Each PNG and .mat gets a .meta; an existing .meta keeps its GUID. Textures/structures_trim_manifest.json lists every file.
"""
import argparse
import hashlib
import json
import os
import re
import sys
import uuid

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
sys.path.insert(0, os.path.join(HERE, "..", "pool_tile"))
import gen_pool_tile as pt   # noqa: E402  (noise, blur, normals, metas: the same tools as the pool tile)

SURFACE_GUID = "38dd6d7df75f84369b8237ac8848106c"   # FrontRooms/Surface
URP_LIT_GUID = "933532a4fcc9baf4fa0491de14d08ed7"   # Universal Render Pipeline/Lit
MACRO_GUID = "f777b2e3dd7d240d390e2b2cd59cbb9e"
SS = 2

STRIPS = ["Sealant", "ShoeAluminium", "PipePaint", "AbrasiveNosing", "WallAngle", "PourStop", "FasciaBand", "CarpetBar",
          "FasciaCap", "SteelAngleNosing", "BullnoseBlockEnd", "CurbAngle", "Sawcut", "Galvanised", "StainlessBrushed", "TBarWhite"]


def srgb_to_lin(c):
    c = np.asarray(c, np.float32)
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def lin_to_srgb(c):
    c = np.clip(c, 0.0, 1.0)
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * np.power(c, 1.0 / 2.4) - 0.055)


def smooth(e0, e1, x):
    return pt.smoothstep(e0, e1, x)


def band(v, a, b, soft):
    """1 inside [a, b] (fractions of the strip's V), soft edges."""
    return smooth(a - soft, a + soft, v) * (1.0 - smooth(b - soft, b + soft, v))


# ------------------------------------------------------------------------------------------------ one strip
def strip(name, W, H, seed):
    """One strip at supersampled size W x H (u along W = 1 m, v across H = the profile, 0 at the strip's bottom row).
    Returns albedo (sRGB 0..1, 3), height (mm), smoothness, cavity, metal."""
    rng = np.random.default_rng(seed)
    mm_u = 1000.0 / W
    u = (np.arange(W, dtype=np.float32) + 0.5) / W
    v = (np.arange(H, dtype=np.float32) + 0.5) / H
    U, V = np.meshgrid(u, v)
    n_lo = pt.noise(H, W, W / 6.0, seed + 1)            # metre-scale tone drift along u
    n_mid = pt.noise(H, W, 40.0 / mm_u, seed + 2)        # 40 mm blotches
    n_fine = pt.noise(H, W, 1.2 / mm_u, seed + 3)        # grit
    # brushing: noise stretched along u (strong along-u correlation)
    br = np.random.default_rng(seed + 5).standard_normal((H, W)).astype(np.float32)
    k = np.exp(-0.5 * (np.fft.fftfreq(W)[None, :] * W / 900.0) ** 2).astype(np.float32)
    br = np.fft.ifft(np.fft.fft(br, axis=1) * k, axis=1).real.astype(np.float32)
    br /= max(float(br.std()), 1e-6)
    edge = np.maximum(1.0 - smooth(0.0, 0.10, V), smooth(0.90, 1.0, V))     # the profile's arrises map near v 0 and 1
    hgt = np.zeros((H, W), np.float32)
    metal = np.zeros((H, W), np.float32)
    cav = np.ones((H, W), np.float32)
    if name == "Sealant":
        base = np.array([0.50, 0.49, 0.46])
        tool = pt.noise(H, W, 30.0 / mm_u, seed + 6) * 0.04
        conc = -0.35 * (1.0 - (2.0 * V - 1.0) ** 2)            # tooled concave across
        hgt = conc + tool + 0.01 * n_fine
        tone = 1.0 + 0.04 * n_mid - 0.10 * edge
        sm = 0.42 + 0.04 * n_mid - 0.10 * edge
        cav = 1.0 - 0.25 * edge
    elif name in ("ShoeAluminium", "FasciaCap", "StainlessBrushed", "CarpetBar", "AbrasiveNosing"):
        base = {"ShoeAluminium": [0.71, 0.72, 0.72], "FasciaCap": [0.27, 0.22, 0.17], "StainlessBrushed": [0.64, 0.65, 0.65],
                "CarpetBar": [0.64, 0.64, 0.62], "AbrasiveNosing": [0.66, 0.66, 0.64]}[name]
        base = np.array(base)
        metal[:] = 1.0
        hgt = 0.006 * br + 0.004 * n_fine
        tone = 1.0 + 0.02 * n_lo + 0.025 * br + 0.02 * n_mid
        sm = {"ShoeAluminium": 0.62, "FasciaCap": 0.58, "StainlessBrushed": 0.72, "CarpetBar": 0.42, "AbrasiveNosing": 0.5}[name] + 0.03 * br - 0.02 * np.abs(n_mid)
        # dust settles low on a shoe and in a cap's lower lip; arrises polished by hands and feet
        dust = smooth(0.25, 0.0, V) * (0.5 + 0.5 * np.clip(n_mid, -1, 1))
        tone = tone * (1.0 - 0.10 * dust) * (1.0 + 0.05 * edge)
        sm = sm - 0.12 * dust + 0.05 * edge
        if name == "CarpetBar":
            # P1 CarpetBar V: lip 0-0.044, front leg 0.044-0.31, nose 0.31-0.40, ribbed top 0.40-0.95, back lip 0.95-1
            top = band(V, 0.40, 0.95, 0.01)
            ribs = 0.5 + 0.5 * np.cos(2.0 * np.pi * (V - 0.40) / (0.55 / 9.0))
            hgt = hgt - 0.35 * top * (ribs < 0.35)
            nose = band(V, 0.31, 0.40, 0.01)
            # FR-7 (K1 review, structures G3-b): a satin anodised bar (0.42, the nose 0.48), never a chrome rod
            sm = sm + 0.06 * nose - 0.10 * top * (ribs < 0.35)
            tone = tone * (1.0 - 0.30 * top * (ribs < 0.35) * (0.6 + 0.4 * np.clip(n_mid, 0, 1))) * (1.0 + 0.06 * nose)
            cav = 1.0 - 0.35 * top * (ribs < 0.35)
        if name == "AbrasiveNosing":
            # P1 AbrasiveNosing V: riser return 0.015-0.21, nose 0.21-0.25, top 0.25-0.975 with three grit bands
            grit = np.zeros_like(V)
            for c in (0.40, 0.60, 0.80):
                grit = np.maximum(grit, band(V, c - 0.055, c + 0.055, 0.006))
            sand = pt.noise(H, W, 0.7 / mm_u, seed + 7)
            hgt = hgt + grit * (0.08 * sand - 0.15)
            metal = metal * (1.0 - grit)
            gcol = np.array([0.16, 0.16, 0.15])
            tone = tone * (1.0 - grit) + grit * (1.0 + 0.25 * np.clip(sand, -1.0, 1.0))
            base = base[None, None, :] * (1.0 - grit[..., None]) + gcol[None, None, :] * grit[..., None]
            sm = sm * (1.0 - grit) + grit * (0.18 + 0.05 * sand)
            cav = 1.0 - 0.25 * grit
    elif name in ("PipePaint", "WallAngle", "FasciaBand", "TBarWhite", "SteelAngleNosing"):
        base = np.array({"PipePaint": [0.42, 0.43, 0.42], "WallAngle": [0.86, 0.86, 0.84], "FasciaBand": [0.80, 0.79, 0.75],
                         "TBarWhite": [0.87, 0.87, 0.85], "SteelAngleNosing": [0.07, 0.07, 0.07]}[name])
        peel = pt.noise(H, W, 2.5 / mm_u, seed + 8) * (0.012 if name == "FasciaBand" else 0.005)
        hgt = peel + 0.004 * n_fine
        tone = 1.0 + 0.02 * n_lo + 0.02 * n_mid
        sm = {"PipePaint": 0.62, "WallAngle": 0.48, "FasciaBand": 0.30, "TBarWhite": 0.46, "SteelAngleNosing": 0.55}[name] + 0.03 * n_mid
        chips = np.zeros((H, W), np.float32)
        if name in ("PipePaint", "SteelAngleNosing"):
            spots = pt.noise(H, W, 6.0 / mm_u, seed + 9)
            chips = (spots > 2.7).astype(np.float32)
            if name == "SteelAngleNosing":
                # the arris (the strip's top edge: the angle's heel) worn through to the steel by feet and carts
                heel = smooth(0.80, 0.97, V)
                chips = np.maximum(chips, heel * (pt.noise(H, W, 4.0 / mm_u, seed + 10) > 0.2 - 1.4 * heel))
            steel = np.array([0.47, 0.46, 0.44])
            base = base[None, None, :] * (1.0 - chips[..., None]) + steel[None, None, :] * chips[..., None]
            metal = chips
            hgt = hgt - 0.05 * chips
            sm = sm * (1.0 - chips) + 0.42 * chips
        if name == "TBarWhite":
            # the tee's face: a crisp rounded edge each side (the hem of the folded steel)
            hgt = hgt + 0.08 * (1.0 - edge)
            tone = tone * (1.0 - 0.06 * edge)
        cav = 1.0 - 0.10 * edge
    elif name in ("PourStop", "Galvanised"):
        base = np.array([0.60, 0.61, 0.60])
        metal[:] = 1.0
        cell = pt.noise(H, W, 18.0 / mm_u, seed + 12)
        tone = 1.0 + 0.06 * np.sign(cell) * np.minimum(np.abs(cell), 1.0) + 0.02 * n_lo
        sm = 0.40 + 0.08 * np.clip(cell, -1, 1)
        hgt = 0.004 * n_fine
        if name == "PourStop":
            splash = (pt.noise(H, W, 15.0 / mm_u, seed + 13) + 1.6 * smooth(0.55, 0.0, V) > 1.4).astype(np.float32)
            ccol = np.array([0.58, 0.57, 0.54])
            base = base[None, None, :] * (1.0 - splash[..., None]) + ccol[None, None, :] * splash[..., None]
            metal = metal * (1.0 - splash)
            sm = sm * (1.0 - splash) + 0.15 * splash
            hgt = hgt + 0.08 * splash * (0.5 + 0.5 * n_fine)
    elif name == "BullnoseBlockEnd":
        base = np.array([0.74, 0.73, 0.70])
        pores = (pt.noise(H, W, 0.8 / mm_u, seed + 14) > 1.6).astype(np.float32)
        rnd = np.sin(np.pi * V)                       # the bullnose: its round across the strip
        hgt = 2.0 * rnd - 0.25 * pores + 0.02 * n_fine
        tone = (1.0 + 0.03 * n_mid + 0.02 * n_lo) * (1.0 - 0.25 * pores)
        sm = 0.28 - 0.06 * pores
        cav = 1.0 - 0.35 * pores
    elif name == "CurbAngle":
        yellow = np.array([0.80, 0.62, 0.10])
        steel = np.array([0.40, 0.39, 0.37])
        rust = np.array([0.38, 0.22, 0.12])
        # the top arris (the strip's top) worn to steel where the carts and the leveler lip strike; scuffs on the face; rust
        # bleeding from the bare steel down the paint
        scuff = pt.noise(H, W, 25.0 / mm_u, seed + 15)
        worn = np.clip(smooth(0.78, 0.97, V) * (0.7 + 0.5 * scuff) + 0.6 * (scuff > 2.0), 0.0, 1.0)
        streak = np.clip(pt.noise(H, W, 9.0 / mm_u, seed + 16), 0, 1) * smooth(0.95, 0.6, V) * smooth(0.0, 0.5, V)
        rusty = np.clip(0.30 * streak + 0.35 * smooth(0.15, 0.0, V) * np.clip(scuff + 0.5, 0, 1), 0.0, 1.0) * (1.0 - worn)
        paint = 1.0 - worn
        col = yellow[None, None, :] * (paint * (1.0 - rusty))[..., None] + steel[None, None, :] * worn[..., None] + rust[None, None, :] * (rusty * paint)[..., None]
        base = col
        metal = worn * 0.9
        tone = 1.0 + 0.04 * n_mid
        sm = 0.45 * paint * (1.0 - rusty) + 0.50 * worn + 0.15 * rusty
        hgt = 0.04 * n_fine - 0.08 * worn + 0.10 * rusty * n_fine
    elif name == "Sawcut":
        base = np.array([0.58, 0.57, 0.54])
        broom = pt.noise(H, W, 1.0, seed + 17)
        kb = np.exp(-0.5 * (np.fft.fftfreq(H)[:, None] * H / 40.0) ** 2).astype(np.float32)
        broom = np.fft.ifft(np.fft.fft(broom, axis=0) * kb, axis=0).real.astype(np.float32)
        broom /= max(float(broom.std()), 1e-6)
        cut = band(V, 0.47, 0.53, 0.008)
        hgt = 0.05 * broom + 0.03 * n_fine - 2.0 * cut
        tone = (1.0 + 0.05 * n_mid + 0.03 * n_lo) * (1.0 - 0.55 * cut)
        sm = 0.22 + 0.04 * n_mid - 0.06 * cut
        cav = 1.0 - 0.6 * cut
    else:
        base = np.array([0.5, 0.5, 0.5])
        tone = np.ones((H, W), np.float32)
        sm = np.full((H, W), 0.4, np.float32)
    if np.ndim(base) == 1:
        base = np.broadcast_to(base[None, None, :], (H, W, 3))
    lin = srgb_to_lin(base) * np.clip(np.asarray(tone)[..., None], 0.0, 2.0)
    alb = lin_to_srgb(lin)
    return alb.astype(np.float32), hgt.astype(np.float32), np.clip(sm, 0.0, 1.0).astype(np.float32), np.clip(cav, 0.0, 1.0).astype(np.float32), np.clip(metal, 0.0, 1.0).astype(np.float32)


# ------------------------------------------------------------------------------------------------ the sheet
def trim_sheet(wd=2048, hd=1024, seed=900):
    W, H = wd * SS, hd * SS
    sh = H // len(STRIPS)
    alb = np.zeros((H, W, 3), np.float32)
    nrm = np.zeros((H, W, 3), np.float32)
    msk = np.zeros((H, W, 4), np.float32)
    for i, name in enumerate(STRIPS):
        a, h, s, c, m = strip(name, W, sh, seed + 31 * i)
        # mm per pixel across the strip: the profile it is made for, about 70 mm for the small ones and up to 0.42 m (shoe)
        mm_v = {"ShoeAluminium": 6.6, "StainlessBrushed": 6.6, "FasciaCap": 2.0, "FasciaBand": 8.0, "PourStop": 2.5,
                "Galvanised": 3.0, "CurbAngle": 2.5, "Sawcut": 1.0}.get(name, 1.1)
        n = pt.normals(h, 1000.0 / W, mm_v, wrap_v=False)
        # rows: strip i from the top of the map (row 0 of the array is the map's bottom, V 0)
        r0 = H - (i + 1) * sh
        alb[r0:r0 + sh] = a
        nrm[r0:r0 + sh] = n
        msk[r0:r0 + sh, :, 0] = s
        msk[r0:r0 + sh, :, 1] = c
        msk[r0:r0 + sh, :, 2] = m
    return alb, nrm, msk


def wire_mesh(px=512, seed=950):
    """Woven wire: 4 x 4 diamonds of 38 mm (1 1/2 in) in 152.4 mm, 10 ga wire (3.43 mm), crimped over and under."""
    N = px * SS
    span = 152.4
    mm = span / N
    yy, xx = np.mgrid[0:N, 0:N].astype(np.float32)
    x = (xx + 0.5) * mm
    y = (yy + 0.5) * mm
    pitch = span / 4.0                       # one diamond's width along each diagonal family
    # two families of wires along the diagonals
    p = pitch / np.sqrt(2.0)
    t1 = np.mod((x + y) / np.sqrt(2.0), p)
    t2 = np.mod((x - y) / np.sqrt(2.0), p)
    r = 3.43 * 0.5
    dist1 = np.minimum(t1, p - t1)
    dist2 = np.minimum(t2, p - t2)
    w1 = np.clip(1.0 - (dist1 / r) ** 2, 0.0, 1.0)
    w2 = np.clip(1.0 - (dist2 / r) ** 2, 0.0, 1.0)
    # crimp: each wire rises and falls along its length, over at one crossing and under at the next
    s1 = (x - y) / np.sqrt(2.0)
    s2 = (x + y) / np.sqrt(2.0)
    crimp1 = 0.9 * np.cos(np.pi * s1 / p)
    crimp2 = -0.9 * np.cos(np.pi * s2 / p)
    h1 = np.where(w1 > 0, np.sqrt(w1) * r + crimp1, -9.0)
    h2 = np.where(w2 > 0, np.sqrt(w2) * r + crimp2, -9.0)
    hgt = np.maximum(h1, h2)
    cover = np.maximum(w1 > 0, w2 > 0).astype(np.float32)
    hgt = np.where(cover > 0, hgt, 0.0)
    n = pt.normals(hgt, mm, mm)
    galv = np.array([0.62, 0.63, 0.62]) * (1.0 + 0.05 * pt.noise(N, N, 40.0, seed + 1))[..., None]
    # under-crossings darker (occluded)
    occl = np.where(h1 > h2, np.clip(1.0 - 0.35 * w2 * (h2 > -9), 0.6, 1.0), np.clip(1.0 - 0.35 * w1 * (h1 > -9), 0.6, 1.0))
    alb = np.clip(galv * occl[..., None], 0.0, 1.0)
    return alb.astype(np.float32), cover, n


def fireproofing(px=1024, seed=970):
    N = px * SS
    mm = 1000.0 / N
    lumps = pt.noise(N, N, 14.0 / mm, seed + 1) * 2.2 + pt.noise(N, N, 5.0 / mm, seed + 2) * 1.0 + pt.noise(N, N, 1.6 / mm, seed + 3) * 0.35
    fib = pt.noise(N, N, 0.5 / mm, seed + 4) * 0.12
    hgt = lumps + fib
    n = pt.normals(hgt, mm, mm)
    tone = 1.0 + 0.05 * pt.noise(N, N, 120.0 / mm, seed + 5) + 0.06 * np.clip(lumps / 2.5, -1, 1)
    base = np.array([0.73, 0.72, 0.69])
    alb = lin_to_srgb(srgb_to_lin(base)[None, None, :] * tone[..., None])
    cav = np.clip(1.0 - 0.25 * smooth(0.0, -2.5, lumps), 0.5, 1.0)
    sm = np.clip(0.10 + 0.03 * pt.noise(N, N, 30.0 / mm, seed + 6), 0.0, 1.0)
    dust = np.clip(0.5 + 0.5 * pt.noise(N, N, 200.0 / mm, seed + 7), 0, 1) * 0.15
    alb = alb * (1.0 - dust[..., None] * 0.3)
    return alb.astype(np.float32), n, sm, cav


def access_floor(px=1024, seed=990):
    """2 x 2 panels of 600 mm in 1.2 m: HPL in a light warm grey with a fine chip pattern, each panel its own lot and a quarter
    turn of the pattern, a 2 mm black PVC edge at every panel joint, the panel faces a hair proud of the edge."""
    N = px * SS
    mm = 1200.0 / N
    yy, xx = np.mgrid[0:N, 0:N].astype(np.float32)
    x = (xx + 0.5) * mm
    y = (yy + 0.5) * mm
    pi = (x // 600.0).astype(np.int32)
    pj = (y // 600.0).astype(np.int32)
    lx = x - (pi + 0.5) * 600.0
    ly = y - (pj + 0.5) * 600.0
    pid = pj * 2 + pi
    rng = np.random.default_rng(seed)
    lot = rng.normal(0.0, 0.025, 4).astype(np.float32)
    chips_a = pt.noise(N, N, 1.4 / mm, seed + 1)
    chips_b = np.rot90(chips_a).copy()
    chips = np.where((pid % 2) == 0, chips_a, chips_b)
    fleck = (chips > 1.7).astype(np.float32) - 0.6 * (chips < -1.9).astype(np.float32)
    base = np.array([0.66, 0.65, 0.61])
    tone = (1.0 + lot[pid]) * (1.0 + 0.10 * fleck + 0.015 * pt.noise(N, N, 80.0 / mm, seed + 2))
    edge_d = np.minimum(300.0 - np.abs(lx), 300.0 - np.abs(ly))
    edge = (edge_d < 2.0).astype(np.float32)
    ease = smooth(2.0, 3.5, edge_d)
    pvc = np.array([0.05, 0.05, 0.05])
    lin = srgb_to_lin(base)[None, None, :] * tone[..., None]
    alb = lin_to_srgb(lin) * (1.0 - edge[..., None]) + pvc[None, None, :] * edge[..., None]
    scuff = np.clip(pt.noise(N, N, 60.0 / mm, seed + 3), 0, 1) * 0.12
    alb = alb * (1.0 - scuff[..., None] * 0.2)
    hgt = 0.15 * ease - 0.15 + 0.004 * chips
    n = pt.normals(hgt, mm, mm)
    sm = np.where(edge > 0, 0.35, 0.52 + 0.04 * pt.noise(N, N, 50.0 / mm, seed + 4) - 0.15 * scuff)
    cav = np.where(edge > 0, 0.55, 1.0 - 0.15 * (1.0 - ease))
    return alb.astype(np.float32), n, np.clip(sm, 0, 1).astype(np.float32), np.clip(cav, 0, 1).astype(np.float32)


# ------------------------------------------------------------------------------------------------ files
def down(a, k):
    return pt.down(a, k)


def save_png(path, arr, mode):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    Image.fromarray(np.ascontiguousarray(pt.to8(arr)[::-1]), mode).save(path, optimize=False, compress_level=9)


def tex_meta(png, size, normal, alpha, coverage=False):
    """The meta exactly as Unity writes it for a surface texture after FrontRoomsSurfaceTextureImporter (the game's PaintedMetal_A /
    _N / _S metas as templates, by suffix), with this file's own GUID (kept when a meta exists), so an Editor importing it never
    rewrites it. The woven wire's mips keep its coverage."""
    meta = png + ".meta"
    guid = pt.existing_guid(meta)
    stem = os.path.splitext(os.path.basename(png))[0]
    suffix = stem[-2:]
    tpl = os.path.join(ROOT, "Assets", "Resources", "Surfaces", "Textures", "PaintedMetal%s.png.meta" % suffix)
    t = open(tpl).read()
    t = re.sub(r"^guid: [0-9a-f]{32}", "guid: " + guid, t, count=1, flags=re.M)
    if coverage:
        t = t.replace("mipMapsPreserveCoverage: 0", "mipMapsPreserveCoverage: 1")
    pt.write_text(meta, t)
    return guid


# The phone tier (quality Medium) has no realtime reflection probes, so a metal texel (metallic 1) reflects nothing there and
# P3's aluminium carpet bars read as black strips while the desktop shows them bright (G1 review FIX 3). The phone set takes a
# lit fallback: metal off (the _Phone material's _Metallic 0) and each metal texel's albedo the light its metal shows in a lit
# room, its F0 (the albedo map's colour, linear) times PHONE_LIT_METAL (0.7: a metal facing up mirrors the lit ceiling, one
# facing the room the walls and the floor; a white paint sits at 1). Dielectric texels are unchanged.
PHONE_LIT_METAL = 0.7


def lit_metal(a_srgb, metal, k):
    lin = srgb_to_lin(a_srgb)
    m = np.clip(metal, 0.0, 1.0)[..., None]
    return lin_to_srgb(lin * (1.0 - m) + lin * k * m).astype(np.float32)


def write_set(out_tex, stem, alb, nrm, msk, manifest, alpha_for_a=None, coverage=False, phone_lit=0.0):
    """stem_A (sRGB), stem_N, stem_S at the desktop size and the phone set at half (stemPhone_*). phone_lit > 0: the phone
    albedo carries the lit look of the metal texels (mask B) and its material turns metal off (lit_metal)."""
    guids = {}
    for tier, k in (("", SS), ("Phone", SS * 2)):
        a = down(alb, k)
        if tier == "Phone" and phone_lit > 0.0 and msk is not None:
            a = lit_metal(a, down(msk, k)[..., 2], phone_lit)
        n = down(nrm, k)
        n /= np.linalg.norm(n, axis=-1, keepdims=True)
        nenc = n * 0.5 + 0.5
        files = []
        if alpha_for_a is not None:
            al = down(alpha_for_a[..., None], k)
            files.append(("A", np.concatenate([a, al], -1), "RGBA", False, True))
        else:
            files.append(("A", a, "RGB", False, False))
        files.append(("N", nenc, "RGB", True, False))
        if msk is not None:
            files.append(("S", down(msk, k), "RGBA", False, True))
        for key, arr, mode, is_n, has_a in files:
            fn = "%s%s_%s.png" % (stem, tier, key)
            path = os.path.join(out_tex, fn)
            save_png(path, arr, mode)
            g = tex_meta(path, max(arr.shape[0], arr.shape[1]), is_n, has_a, coverage=coverage and key == "A")
            manifest["textures"][fn] = dict(size=[arr.shape[1], arr.shape[0]], sha1=pt.sha1(path), guid=g, bytes=os.path.getsize(path))
            guids[key + tier] = g
    return guids


SURFACE_MAT = """%YAML 1.1
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
        m_Texture: {{fileID: 2800000, guid: {a}, type: 3}}
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
    m_Ints: []
    m_Floats:
    - _BumpScale: 1
    - _CeilingGrime: 0
    - _CeilingHeight: 2.9
    - _Cull: 2
    - _Cutoff: 0.5
    - _FloorGrime: 0
    - _FloorGrimeHeight: 0.35
    - _MacroDirt: {macro_dirt}
    - _MacroTone: {macro_tone}
    - _MeshUV: 1
    - _Metallic: {metallic}
    - _OcclusionStrength: {occ}
    - _Sheen: 0
    - _Smoothness: 1
    - _StainStrength: 0
    - _UseEmission: 0
    - _UseSheen: {sheen}
    - _UseTile: 0
    - _WaterY: -1000
    - _WetStrength: 0
    m_Colors:
    - _BaseColor: {{r: 1, g: 1, b: 1, a: 1}}
    - _EmissionColor: {{r: 0, g: 0, b: 0, a: 1}}
    - _StainColor: {{r: 0.42, g: 0.31, b: 0.16, a: 1}}
    - _TileSize: {{r: {tile}, g: {tile}, b: 0, a: 0}}
  m_BuildTextureStacks: []
  m_AllowLocking: 1
"""

WIRE_MAT = """%YAML 1.1
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
  - _ALPHATEST_ON
  - _NORMALMAP
  m_InvalidKeywords: []
  m_LightmapFlags: 4
  m_EnableInstancingVariants: 0
  m_DoubleSidedGI: 1
  m_CustomRenderQueue: 2450
  stringTagMap:
    RenderType: TransparentCutout
  disabledShaderPasses:
  - MOTIONVECTORS
  m_LockedProperties: 
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs:
    - _BaseMap:
        m_Texture: {{fileID: 2800000, guid: {a}, type: 3}}
        m_Scale: {{x: {tiling}, y: {tiling}}}
        m_Offset: {{x: 0, y: 0}}
    - _BumpMap:
        m_Texture: {{fileID: 2800000, guid: {n}, type: 3}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _MainTex:
        m_Texture: {{fileID: 2800000, guid: {a}, type: 3}}
        m_Scale: {{x: {tiling}, y: {tiling}}}
        m_Offset: {{x: 0, y: 0}}
    m_Ints: []
    m_Floats:
    - _AlphaClip: 1
    - _AlphaToMask: 1
    - _Blend: 0
    - _BumpScale: 1
    - _Cull: 0
    - _Cutoff: 0.5
    - _DstBlend: 0
    - _EnvironmentReflections: 1
    - _Metallic: 0.55
    - _OcclusionStrength: 1
    - _QueueOffset: 0
    - _ReceiveShadows: 1
    - _Smoothness: 0.42
    - _SpecularHighlights: 1
    - _SrcBlend: 1
    - _Surface: 0
    - _WorkflowMode: 1
    - _ZWrite: 1
    m_Colors:
    - _BaseColor: {{r: 1, g: 1, b: 1, a: 1}}
    - _Color: {{r: 1, g: 1, b: 1, a: 1}}
    - _EmissionColor: {{r: 0, g: 0, b: 0, a: 1}}
  m_BuildTextureStacks: []
  m_AllowLocking: 1
"""


def write_mat(path, text, manifest):
    pt.write_text(path, text)
    g = pt.existing_guid(path + ".meta")
    pt.write_text(path + ".meta", pt.META_MAT.format(guid=g))
    manifest["materials"][os.path.basename(path)[:-4]] = dict(guid=g, sha1=pt.sha1(path))


def surface_mat(out, name, g, tier, keywords, metallic, sheen, tile, macro_tone, macro_dirt, occ, manifest):
    kw = "".join("  - %s\n" % k for k in keywords)
    text = SURFACE_MAT.format(name=name, shader=SURFACE_GUID, keywords=kw, a=g["A" + tier], n=g["N" + tier], s=g["S" + tier], macro=MACRO_GUID,
                              metallic=metallic, sheen=sheen, tile=tile, macro_tone=macro_tone, macro_dirt=macro_dirt, occ=occ)
    write_mat(os.path.join(out, name + ".mat"), text, manifest)


# Prop_RailEnamel (G1 review FIX 4): the scene's Painted_Metal (Level 0's steel, Level0.cs) as a handrail's enamel. Its own maps
# (1024 px a metre: 1 mm a texel, made for doors and frames) read at the 0.35 m hero as lumpy plaster on a 42 mm tube, magnified
# about 3.3 times; here a tile is 0.30 m (0.29 mm a texel: the same orange peel at a rail's scale), the gloss of a baked enamel
# (the mask's 0.52 x 1.2 = 0.62) and the peel a little flatter (bump 0.8). No new map: Painted_Metal.mat's own properties and
# textures, read from the project, with those three changed; the painted rail kits (P4 _Painted) and K1's rails wear it.
def rail_enamel(out, manifest):
    src = os.path.join(ROOT, "Assets", "Resources", "Surfaces", "Painted_Metal.mat")
    t = open(src).read()
    t = t.replace("m_Name: Painted_Metal", "m_Name: Prop_RailEnamel", 1)
    t = re.sub(r"- _TileSize: \{r: [0-9.]+, g: [0-9.]+,", "- _TileSize: {r: 0.3, g: 0.3,", t, count=1)
    t = re.sub(r"- _Smoothness: [0-9.]+", "- _Smoothness: 1.2", t, count=1)
    t = re.sub(r"- _BumpScale: [0-9.]+", "- _BumpScale: 0.8", t, count=1)
    assert "Prop_RailEnamel" in t and "_TileSize: {r: 0.3, g: 0.3," in t and "_Smoothness: 1.2" in t and "_BumpScale: 0.8" in t
    write_mat(os.path.join(out, "Prop_RailEnamel.mat"), t, manifest)


def preview(out_dir, name, alb):
    os.makedirs(out_dir, exist_ok=True)
    a = pt.to8(alb[::-1])
    Image.fromarray(a[..., :3] if a.shape[-1] >= 3 else a, "RGB").save(os.path.join(out_dir, "preview_%s.png" % name))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default=os.path.join(ROOT, "Assets", "Resources", "Surfaces"))
    ap.add_argument("--preview", default="")
    a = ap.parse_args()
    out_tex = os.path.join(a.out, "Textures")
    man_path = os.path.join(out_tex, "structures_trim_manifest.json")
    manifest = dict(textures={}, materials={}, strips=STRIPS)
    # the trim sheet
    alb, nrm, msk = trim_sheet()
    g = write_set(out_tex, "StructTrim", alb, nrm, msk, manifest, phone_lit=PHONE_LIT_METAL)
    for tier, suffix in (("", ""), ("Phone", "_Phone")):
        surface_mat(a.out, "Prop_StructTrim" + suffix, g, tier, ["_FR_MESH_UV", "_FR_SHEEN"], 1 if tier == "" else 0, 1, 1, 0.06, 0.10, 0.9, manifest)
    if a.preview:
        preview(a.preview, "StructTrim", down(alb, SS))
    print("trim sheet 2048 x 1024 (+ phone 1024 x 512)", flush=True)
    # woven wire
    walb, cover, wn = wire_mesh()
    g = write_set(out_tex, "WireMesh", walb, wn, None, manifest, alpha_for_a=cover, coverage=True)
    write_mat(os.path.join(a.out, "Prop_WireMesh.mat"), WIRE_MAT.format(name="Prop_WireMesh", shader=URP_LIT_GUID, a=g["A"], n=g["N"], tiling="6.5617"), manifest)
    if a.preview:
        preview(a.preview, "WireMesh", down(walb, SS) * down(cover[..., None], SS))
    print("woven wire 512 x 512", flush=True)
    # fireproofing
    falb, fn_, fsm, fcav = fireproofing()
    fmsk = np.stack([fsm, fcav, np.zeros_like(fsm), np.zeros_like(fsm)], -1)
    g = write_set(out_tex, "Fireproofing", falb, fn_, fmsk, manifest)
    for tier, suffix in (("", ""), ("Phone", "_Phone")):
        surface_mat(a.out, "Prop_Fireproofing" + suffix, g, tier, ["_FR_MESH_UV"], 0, 0, 1, 0.10, 0.18, 1, manifest)
    if a.preview:
        preview(a.preview, "Fireproofing", down(falb, SS))
    print("fireproofing 1024 x 1024", flush=True)
    # access floor (P8)
    aalb, an, asm, acav = access_floor()
    amsk = np.stack([asm, acav, np.zeros_like(asm), np.zeros_like(asm)], -1)
    g = write_set(out_tex, "AccessFloor", aalb, an, amsk, manifest)
    for tier, suffix in (("", ""), ("Phone", "_Phone")):
        surface_mat(a.out, "Prop_AccessFloor" + suffix, g, tier, ["_FR_MESH_UV"], 0, 0, 1.2, 0.08, 0.12, 0.9, manifest)
    if a.preview:
        preview(a.preview, "AccessFloor", down(aalb, SS))
    print("access floor 1024 x 1024", flush=True)
    rail_enamel(a.out, manifest)
    print("rail enamel (Painted_Metal's maps at 0.30 m, enamel gloss)", flush=True)
    pt.write_text(man_path, json.dumps(manifest, indent=1, sort_keys=True) + "\n")
    if not os.path.exists(man_path + ".meta"):
        pt.write_text(man_path + ".meta", "fileFormatVersion: 2\nguid: %s\nTextScriptImporter:\n  externalObjects: {}\n  userData: \n"
                                          "  assetBundleName: \n  assetBundleVariant: \n" % uuid.uuid4().hex)
    print("done: %d textures, %d materials" % (len(manifest["textures"]), len(manifest["materials"])))


if __name__ == "__main__":
    main()
