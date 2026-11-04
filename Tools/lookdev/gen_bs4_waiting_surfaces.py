#!/usr/bin/env python3
"""The six new Surfaces of the BS4 hero Waiting kits and the magazine-cover placeholder atlas (游戏视觉,
bs4-hero-kits, builder A; research/station/72_hero_kits.md §3).

Writes, under <out-root>/Assets/Resources/Surfaces/:
  Prop_VinylSlate / Prop_VinylNavy / Prop_VinylCharcoal  .mat + .mat.meta   tinted Prop_Vinyl maps (by GUID) at a 0.45 m tile (upholstery grain at 0.35 m)
  Prop_AshSand            .mat + .mat.meta   #8C877C, smoothness 0.08, Prop_FabricCharcoal_N at 0.6, tile 0.25 m
  Prop_SignNoSmoking      .mat + .mat.meta   white x Prop_SignNoSmoking_A with its engraving _N and mask _S (平面视觉 · 信息设计's
                          plate, 11-04 06:23 / 07:02); smoothness scale 1, so the mask gives 0.45 face / 0.25 letters
  Prop_MagazineCover      .mat + .mat.meta   white x Prop_MagazineCover_A, smoothness 0.6 (coated stock)
  Textures/Prop_MagazineCover_A.png + .png.meta   1024^2, 2 x 2 cells of 512^2, cell n row-major from the top-left: the
                          covers are 平面视觉 · 信息设计's art (landed 11-04 06:58 over the placeholder), never written
                          over; --placeholder-covers draws the flat placeholder only where no atlas exists yet

Every .mat is Prop_Vinyl.mat's text (FrontRooms/Surface 38dd6d7d..., keyword _FR_MESH_UV, _MeshUV 1) with a new
m_Name, its own maps and values; every .meta gets a fresh GUID (uuid4) unless the file already exists in the
out-root (re-runs keep their GUIDs). LF only. The cover placeholder is four flat period colour fields: a masthead
band, a photo field and blank cover-line bars, NO lettering (type and strings are 平面视觉's and 叙事's), no red, no
glow-ink yellow-green, no amber. Each cover maps its whole cell (0.216 x 0.279 m on 512 x 512 px: 2,370 px/m
across, 1,835 px/m down), so real art is drawn at 512 x 661 per cover and resized to the cell.

Usage: python3 gen_bs4_waiting_surfaces.py --src <project with Prop_Vinyl.mat> --out-root <root> [--check] [--placeholder-covers]
Needs Pillow and numpy (W/venv).
"""
import argparse
import os
import re
import sys
import uuid

GUID_VINYL_N = "bd17a764f591e40dbaa68a4d708fa48b"
GUID_VINYL_S = "a6042614b72a94baaa3de6587d13f73f"
GUID_MACRO = "f777b2e3dd7d240d390e2b2cd59cbb9e"
GUID_NO_SMOKING_A = "01feb813bc154c0bb1800928fbb3fc43"   # Surfaces/Textures/Prop_SignNoSmoking_A.png (平面视觉 · 信息设计, 11-04)
GUID_NO_SMOKING_N = "f4de404f2edf4b75a82359dfa8702635"   # its engraving: letters 0.3 mm deep (W10 recipe, 11-04 07:02)
GUID_NO_SMOKING_S = "d0604b4c1117426ca79cd11656020a8c"   # R smoothness 0.45 face / 0.25 letters
GUID_FABRIC_N = "53385237db6b54a81b6d4b89f6a36b94"

MATS = {
    # name: (hex, smoothness, bump_scale, tile, maps {prop: guid or None}, macro_dirt, macro_tone)
    "Prop_VinylSlate": ("566679", 1.0, 1.0, 0.45, {"_BaseMap": None, "_BumpMap": GUID_VINYL_N, "_MaskMap": GUID_VINYL_S, "_MacroMap": GUID_MACRO}, None, None),
    "Prop_VinylNavy": ("26324A", 1.0, 1.0, 0.45, {"_BaseMap": None, "_BumpMap": GUID_VINYL_N, "_MaskMap": GUID_VINYL_S, "_MacroMap": GUID_MACRO}, None, None),
    "Prop_VinylCharcoal": ("3C3F44", 1.0, 1.0, 0.45, {"_BaseMap": None, "_BumpMap": GUID_VINYL_N, "_MaskMap": GUID_VINYL_S, "_MacroMap": GUID_MACRO}, None, None),
    "Prop_AshSand": ("8C877C", 0.08, 0.6, 0.25, {"_BaseMap": None, "_BumpMap": GUID_FABRIC_N, "_MaskMap": None, "_MacroMap": GUID_MACRO}, 0.0, 0.0),
    "Prop_SignNoSmoking": ("FFFFFF", 1.0, 1.0, 1.0, {"_BaseMap": GUID_NO_SMOKING_A, "_BumpMap": GUID_NO_SMOKING_N, "_MaskMap": GUID_NO_SMOKING_S, "_MacroMap": None}, 0.0, 0.0),
    "Prop_MagazineCover": ("FFFFFF", 0.6, 1.0, 1.0, {"_BaseMap": "COVER", "_BumpMap": None, "_MaskMap": None, "_MacroMap": GUID_MACRO}, 0.03, 0.02),
}

MAT_META = """fileFormatVersion: 2
guid: {guid}
NativeFormatImporter:
  externalObjects: {{}}
  mainObjectFileID: 2100000
  userData:
  assetBundleName:
  assetBundleVariant:
"""

# Cover placeholder: (paper, masthead band, photo field, bar tone, layout) per cell; sRGB.
COVERS = [
    # 0: general-interest monthly, white stock, navy masthead, slate-teal photo
    ((236, 232, 222), (46, 58, 85), (79, 107, 115), (190, 186, 176), "photo_full"),
    # 1: business weekly, cream border, deep green masthead, warm grey photo
    ((226, 220, 203), (47, 74, 60), (122, 106, 88), (96, 92, 84), "photo_inset"),
    # 2: news weekly, slate-blue border, putty photo
    ((214, 210, 200), (74, 93, 122), (165, 156, 140), (74, 93, 122), "border"),
    # 3: home and garden, white masthead band, olive photo
    ((240, 237, 230), (230, 226, 216), (111, 127, 90), (232, 229, 220), "photo_bleed"),
]


def guid_for(meta_path):
    if os.path.isfile(meta_path):
        m = re.search(r"guid: ([0-9a-f]{32})", open(meta_path).read())
        if m:
            return m.group(1)
    return uuid.uuid4().hex


def write_lf(path, text):
    with open(path, "w", newline="\n") as fh:
        fh.write(text)


def set_tex(txt, prop, guid):
    pat = re.compile(r"(- " + re.escape(prop) + r":\n\s+m_Texture: )\{[^}]*\}")
    rep = "{fileID: 0}" if guid is None else "{fileID: 2800000, guid: %s, type: 3}" % guid
    txt2, n = pat.subn(lambda m: m.group(1) + rep, txt)
    assert n == 1, prop
    return txt2


def set_float(txt, prop, value):
    txt2, n = re.subn(r"(- " + re.escape(prop) + r": )[-0-9.eE+]+", lambda m: m.group(1) + ("%g" % value), txt)
    assert n == 1, prop
    return txt2


def set_colour(txt, prop, rgba):
    txt2, n = re.subn(r"(- " + re.escape(prop) + r": )\{[^}]*\}", lambda m: m.group(1) + "{r: %.4f, g: %.4f, b: %.4f, a: %.4f}" % rgba, txt)
    assert n == 1, prop
    return txt2


def cover_atlas():
    import numpy as np
    from PIL import Image, ImageDraw
    S, N = 1024, 512
    img = Image.new("RGB", (S, S), (255, 255, 255))
    d = ImageDraw.Draw(img)
    # Draw each cover in cover space (512 x 661) and squash it to the 512^2 cell.
    for i, (paper, mast, photo, bar, layout) in enumerate(COVERS):
        cw, ch = 512, 661
        cv = Image.new("RGB", (cw, ch), paper)
        cd = ImageDraw.Draw(cv)
        if layout == "photo_full":
            cd.rectangle((0, 120, cw, ch), fill=photo)
            cd.rectangle((0, 0, cw, 112), fill=paper)
            cd.rectangle((28, 22, cw - 28, 96), fill=mast)
            for k, (x0, w) in enumerate(((28, 210), (28, 160), (28, 190))):
                y = 420 + k * 44
                cd.rectangle((x0, y, x0 + w, y + 18), fill=bar)
        elif layout == "photo_inset":
            cd.rectangle((0, 0, cw, 104), fill=mast)
            cd.rectangle((36, 136, cw - 36, 470), fill=photo)
            for k, w in enumerate((300, 240, 270, 210)):
                y = 500 + k * 36
                cd.rectangle((36, y, 36 + w, y + 16), fill=bar)
        elif layout == "border":
            cd.rectangle((0, 0, cw, ch), fill=mast)
            cd.rectangle((26, 26, cw - 26, ch - 26), fill=paper)
            cd.rectangle((26, 26, cw - 26, 120), fill=mast)
            cd.rectangle((44, 140, cw - 44, ch - 44), fill=photo)
            cd.rectangle((cw - 170, 150, cw - 54, 250), fill=paper)
        else:  # photo_bleed
            cd.rectangle((0, 0, cw, ch), fill=photo)
            cd.rectangle((0, 0, cw, 118), fill=mast)
            cd.rectangle((30, 26, cw - 30, 92), fill=(200, 196, 186))
            for k, w in enumerate((220, 180)):
                y = 560 + k * 40
                cd.rectangle((30, y, 30 + w, y + 18), fill=bar)
        # a faint stock grain so the flat fields do not band under the vinyl-less light
        a = np.asarray(cv).astype(np.float32)
        rng = np.random.default_rng(1990 + i)
        a += rng.normal(0.0, 1.6, a.shape[:2])[..., None]
        cv = Image.fromarray(np.clip(a, 0, 255).astype(np.uint8))
        cell = cv.resize((N, N), Image.LANCZOS)
        c, r = i % 2, i // 2
        img.paste(cell, (c * N, r * N))
    return img


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--src", required=True)
    ap.add_argument("--out-root", required=True)
    ap.add_argument("--check", action="store_true")
    ap.add_argument("--placeholder-covers", action="store_true")
    a = ap.parse_args()
    template = open(os.path.join(a.src, "Assets/Resources/Surfaces/Prop_Vinyl.mat")).read()
    tex_meta_src = open(os.path.join(a.src, "Assets/Resources/Surfaces/Textures/Prop_StationKeyPlate_A.png.meta")).read()
    sd = os.path.join(a.out_root, "Assets/Resources/Surfaces")
    td = os.path.join(sd, "Textures")
    os.makedirs(td, exist_ok=True)
    png = os.path.join(td, "Prop_MagazineCover_A.png")
    src_png = os.path.join(a.src, "Assets/Resources/Surfaces/Textures/Prop_MagazineCover_A.png")
    # The atlas the .mat names: the out-root's, else --src's (the real covers), so the .mat is the same either way.
    cover_guid = guid_for(png + ".meta") if os.path.isfile(png + ".meta") else guid_for(src_png + ".meta")
    written = []
    if not os.path.isfile(png) and not os.path.isfile(src_png) and not a.placeholder_covers:
        print("WARNING no Prop_MagazineCover_A.png in --out-root or --src: the .mat names a GUID with no atlas (add --placeholder-covers for a placeholder)")
    if not a.check and a.placeholder_covers and not os.path.isfile(png):
        cover_atlas().save(png, optimize=True)
        meta = re.sub(r"guid: [0-9a-f]{32}", "guid: " + cover_guid, tex_meta_src, count=1)
        write_lf(png + ".meta", meta)
        written += [png, png + ".meta"]
    for name, (hx, sm, bump, tile, maps, mdirt, mtone) in MATS.items():
        txt = template.replace("m_Name: Prop_Vinyl\n", "m_Name: %s\n" % name, 1)
        for prop, g in maps.items():
            txt = set_tex(txt, prop, cover_guid if g == "COVER" else g)
        rgb = tuple(int(hx[i:i + 2], 16) / 255.0 for i in (0, 2, 4))
        txt = set_colour(txt, "_BaseColor", rgb + (1.0,))
        txt = set_float(txt, "_Smoothness", sm)
        txt = set_float(txt, "_BumpScale", bump)
        txt = set_colour(txt, "_TileSize", (tile, tile, 0.0, 0.0))
        if mdirt is not None:
            txt = set_float(txt, "_MacroDirt", mdirt)
        if mtone is not None:
            txt = set_float(txt, "_MacroTone", mtone)
        mp = os.path.join(sd, name + ".mat")
        g = guid_for(mp + ".meta")
        if a.check:
            ok = os.path.isfile(mp) and open(mp).read() == txt
            print(("OK   " if ok else "DIFF ") + mp)
            continue
        write_lf(mp, txt)
        write_lf(mp + ".meta", MAT_META.format(guid=g))
        written += [mp, mp + ".meta"]
    for p in written:
        print("wrote", p)


if __name__ == "__main__":
    sys.exit(main())
